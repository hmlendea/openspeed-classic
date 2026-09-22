using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenSpeed.Classic.Tracks;

namespace OpenSpeed.Classic.Rendering.Tracks
{
    public sealed class TrackRenderer(GraphicsDevice graphicsDevice) : ITrackRenderer
    {
        private readonly BasicEffect colourEffect = new(graphicsDevice)
        {
            VertexColorEnabled = true
        };
        private readonly BasicEffect horizonColourEffect = new(graphicsDevice)
        {
            VertexColorEnabled = true
        };
        private readonly RasterizerState horizonRasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly BasicEffect horizonTextureEffect = new(graphicsDevice)
        {
            TextureEnabled = true,
            VertexColorEnabled = true
        };
        private readonly RasterizerState rasterizerState = new()
        {
            CullMode = CullMode.CullClockwiseFace
        };
        private readonly RasterizerState roadMarkingRasterizerState = new()
        {
            CullMode = CullMode.None
        };
        private readonly AlphaTestEffect textureEffect = new(graphicsDevice)
        {
            AlphaFunction = CompareFunction.Greater,
            ReferenceAlpha = 0x10,
            VertexColorEnabled = true
        };

        private Dictionary<int, Vector3> blockCentres = [];
        private TrackColourBatch[] colourBatches = [];
        private TrackColourBatch? horizonDomeBatch;
        private TrackTextureBatch? horizonPanoramaBatch;
        private TrackColourBatch? horizonRingBatch;
        private TrackTextureResource? horizonTextureResource;
        private TrackColourBatch[] roadMarkingBatches = [];
        private TrackTextureBatch[] textureBatches = [];
        private Dictionary<int, TrackTextureResource> textureResources = [];
        private Dictionary<int, HashSet<int>> visibleBlockIdentifiersByBlock = [];
        private bool isDisposed;

        public bool HasGeometry =>
            colourBatches.Length > 0 ||
            roadMarkingBatches.Length > 0 ||
            textureBatches.Length > 0;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            DisposeTrackResources();
            horizonRasterizerState.Dispose();
            horizonColourEffect.Dispose();
            horizonTextureEffect.Dispose();
            rasterizerState.Dispose();
            roadMarkingRasterizerState.Dispose();
            colourEffect.Dispose();
            textureEffect.Dispose();
            isDisposed = true;
        }

        public void Draw(
            TrackCamera camera,
            int viewportWidth,
            int viewportHeight)
        {
            ArgumentNullException.ThrowIfNull(camera);
            ThrowIfDisposed();

            if (!HasGeometry || viewportWidth <= 0 || viewportHeight <= 0)
            {
                return;
            }

            Matrix view = camera.CreateView();
            Matrix projection = camera.CreateProjection(viewportWidth, viewportHeight);
            DrawHorizon(camera.Position, view, projection);
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = rasterizerState;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            ConfigureEffect(colourEffect, view, projection);
            ConfigureEffect(textureEffect, view, projection);
            BoundingFrustum viewFrustum = new(view * projection);
            HashSet<int>? visibleBlockIdentifiers = ResolveVisibleBlockIdentifiers(
                camera.Position);
            DrawColourBatches(
                colourBatches,
                camera.Position,
                viewFrustum,
                visibleBlockIdentifiers);
            DrawTextureBatches(
                camera.Position,
                viewFrustum,
                visibleBlockIdentifiers);
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = roadMarkingRasterizerState;
            DrawColourBatches(
                roadMarkingBatches,
                camera.Position,
                viewFrustum,
                visibleBlockIdentifiers);
        }

        public void Load(LoadedTrack track)
        {
            ArgumentNullException.ThrowIfNull(track);
            ThrowIfDisposed();
            DisposeTrackResources();
            LoadHorizon(track.Horizon);

            TrackBlock[] trackBlocks = track.Blocks.ToArray();
            blockCentres = trackBlocks.ToDictionary(
                trackBlock => trackBlock.Identifier,
                trackBlock => ToVector3(trackBlock.Centre));
            visibleBlockIdentifiersByBlock = [];

            foreach (TrackBlock trackBlock in trackBlocks)
            {
                if (trackBlock.VisibleBlockIdentifiers is null)
                {
                    continue;
                }

                HashSet<int> visibleBlockIdentifiers =
                    [.. trackBlock.VisibleBlockIdentifiers];
                visibleBlockIdentifiers.Add(trackBlock.Identifier);
                visibleBlockIdentifiersByBlock.Add(
                    trackBlock.Identifier,
                    visibleBlockIdentifiers);
            }

            textureResources = track.Textures
                .Select(trackTexture => new TrackTextureResource(
                    graphicsDevice,
                    trackTexture))
                .ToDictionary(
                    textureResource => textureResource.Identifier,
                    textureResource => textureResource);
            Dictionary<int, TrackMaterial> materials = track.Materials
                .ToDictionary(
                    material => material.Identifier,
                    material => material);
            HashSet<int> availableTextureIdentifiers = [.. textureResources.Keys];
            Dictionary<TrackRenderBatchKey, List<VertexPositionColorTexture>>
                texturedVertices = [];
            Dictionary<TrackRenderBatchKey, List<VertexPositionColor>> colouredVertices = [];
            Dictionary<TrackRenderBatchKey, List<VertexPositionColor>>
                roadMarkingVertices = [];

            foreach (TrackBlock trackBlock in trackBlocks)
            {
                AddSurfaces(
                    trackBlock.Identifier,
                    trackBlock.Surfaces,
                    materials,
                    availableTextureIdentifiers,
                    texturedVertices,
                    colouredVertices);
                AddSurfaces(
                    trackBlock.Identifier,
                    trackBlock.ScenerySurfaces,
                    materials,
                    availableTextureIdentifiers,
                    texturedVertices,
                    colouredVertices);
                AddRoadMarkings(
                    trackBlock.Identifier,
                    trackBlock.RoadMarkings,
                    roadMarkingVertices);
            }

            AddSurfaces(
                -1,
                track.ScenerySurfaces,
                materials,
                availableTextureIdentifiers,
                texturedVertices,
                colouredVertices);
            colourBatches = CreateColourBatches(colouredVertices);
            roadMarkingBatches = CreateColourBatches(roadMarkingVertices);
            textureBatches = CreateTextureBatches(texturedVertices);
        }

        private static void AddRoadMarkings(
            int blockIdentifier,
            IEnumerable<TrackRoadMarking> roadMarkings,
            Dictionary<TrackRenderBatchKey, List<VertexPositionColor>> vertices)
        {
            TrackRenderBatchKey batchKey = new(
                blockIdentifier,
                TrackGeometryDetailLevel.High,
                -1);

            foreach (TrackRoadMarking roadMarking in roadMarkings)
            {
                VertexPositionColor[] markingVertices =
                    TrackRoadMarkingVertexBuilder.Build(roadMarking).ToArray();

                if (markingVertices.Length == 0)
                {
                    continue;
                }

                if (!vertices.ContainsKey(batchKey))
                {
                    vertices.Add(batchKey, []);
                }

                vertices[batchKey].AddRange(markingVertices);
            }
        }

        private static void AddSurfaces(
            int blockIdentifier,
            IEnumerable<TrackSurface> surfaces,
            Dictionary<int, TrackMaterial> materials,
            HashSet<int> availableTextureIdentifiers,
            Dictionary<TrackRenderBatchKey, List<VertexPositionColorTexture>>
                texturedVertices,
            Dictionary<TrackRenderBatchKey, List<VertexPositionColor>> colouredVertices)
        {
            foreach (TrackSurface surface in surfaces)
            {
                bool materialWasResolved = materials.TryGetValue(
                    surface.MaterialIdentifier,
                    out TrackMaterial? material);

                if (!materialWasResolved ||
                    material is null ||
                    !availableTextureIdentifiers.Contains(material.TextureIdentifier))
                {
                    TrackRenderBatchKey colourBatchKey = new(
                        blockIdentifier,
                        surface.DetailLevel,
                        -1);

                    if (!colouredVertices.ContainsKey(colourBatchKey))
                    {
                        colouredVertices.Add(colourBatchKey, []);
                    }

                    colouredVertices[colourBatchKey].AddRange(
                        TrackSurfaceVertexBuilder.BuildColoured(surface));

                    continue;
                }

                TrackRenderBatchKey textureBatchKey = new(
                    blockIdentifier,
                    surface.DetailLevel,
                    material.TextureIdentifier);

                if (!texturedVertices.ContainsKey(textureBatchKey))
                {
                    texturedVertices.Add(textureBatchKey, []);
                }

                texturedVertices[textureBatchKey].AddRange(
                    TrackSurfaceVertexBuilder.BuildTextured(surface, material));
            }
        }

        private static void ConfigureEffect(
            BasicEffect effect,
            Matrix view,
            Matrix projection)
        {
            effect.World = Matrix.Identity;
            effect.View = view;
            effect.Projection = projection;
        }

        private static void ConfigureEffect(
            AlphaTestEffect effect,
            Matrix view,
            Matrix projection)
        {
            effect.World = Matrix.Identity;
            effect.View = view;
            effect.Projection = projection;
        }

        private TrackColourBatch[] CreateColourBatches(
            Dictionary<TrackRenderBatchKey, List<VertexPositionColor>> colouredVertices)
        {
            List<TrackColourBatch> batches = [];

            foreach (KeyValuePair<TrackRenderBatchKey, List<VertexPositionColor>> pair in
                colouredVertices
                    .OrderBy(pair => pair.Key.BlockIdentifier)
                    .ThenBy(pair => pair.Key.DetailLevel))
            {
                batches.Add(new TrackColourBatch(
                    graphicsDevice,
                    pair.Key.BlockIdentifier,
                    pair.Key.DetailLevel,
                    ResolveBlockCentre(pair.Key.BlockIdentifier),
                    [.. pair.Value]));
            }

            return [.. batches];
        }

        private TrackTextureBatch[] CreateTextureBatches(
            Dictionary<TrackRenderBatchKey, List<VertexPositionColorTexture>>
                texturedVertices)
        {
            List<TrackTextureBatch> batches = [];

            foreach (
                KeyValuePair<TrackRenderBatchKey, List<VertexPositionColorTexture>> pair in
                texturedVertices
                    .OrderBy(pair => pair.Key.BlockIdentifier)
                    .ThenBy(pair => pair.Key.DetailLevel)
                    .ThenBy(pair => pair.Key.TextureIdentifier))
            {
                if (!textureResources.ContainsKey(pair.Key.TextureIdentifier))
                {
                    continue;
                }

                batches.Add(new TrackTextureBatch(
                    graphicsDevice,
                    pair.Key.BlockIdentifier,
                    pair.Key.DetailLevel,
                    ResolveBlockCentre(pair.Key.BlockIdentifier),
                    pair.Key.TextureIdentifier,
                    [.. pair.Value]));
            }

            return [.. batches];
        }

        private void DisposeTrackResources()
        {
            horizonDomeBatch?.Dispose();
            horizonPanoramaBatch?.Dispose();
            horizonRingBatch?.Dispose();
            horizonTextureResource?.Dispose();

            foreach (TrackColourBatch currentColourBatch in colourBatches)
            {
                currentColourBatch.Dispose();
            }

            foreach (TrackColourBatch roadMarkingBatch in roadMarkingBatches)
            {
                roadMarkingBatch.Dispose();
            }

            foreach (TrackTextureBatch textureBatch in textureBatches)
            {
                textureBatch.Dispose();
            }

            foreach (TrackTextureResource textureResource in textureResources.Values)
            {
                textureResource.Dispose();
            }

            blockCentres = [];
            colourBatches = [];
            horizonDomeBatch = null;
            horizonPanoramaBatch = null;
            horizonRingBatch = null;
            horizonTextureResource = null;
            roadMarkingBatches = [];
            textureBatches = [];
            textureResources = [];
            visibleBlockIdentifiersByBlock = [];
        }

        private void DrawHorizon(
            Vector3 cameraPosition,
            Matrix view,
            Matrix projection)
        {
            if (horizonDomeBatch is null &&
                horizonPanoramaBatch is null &&
                horizonRingBatch is null)
            {
                return;
            }

            Matrix world = Matrix.CreateTranslation(cameraPosition);
            horizonColourEffect.World = world;
            horizonColourEffect.View = view;
            horizonColourEffect.Projection = projection;
            horizonTextureEffect.World = world;
            horizonTextureEffect.View = view;
            horizonTextureEffect.Projection = projection;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = horizonRasterizerState;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

            if (horizonDomeBatch is not null)
            {
                foreach (EffectPass pass in horizonColourEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(horizonDomeBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        horizonDomeBatch.PrimitiveCount);
                }
            }

            if (horizonRingBatch is not null)
            {
                foreach (EffectPass pass in horizonColourEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(horizonRingBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        horizonRingBatch.PrimitiveCount);
                }
            }

            if (horizonPanoramaBatch is not null && horizonTextureResource is not null)
            {
                graphicsDevice.BlendState = BlendState.Opaque;
                horizonTextureEffect.Texture = horizonTextureResource.Texture;

                foreach (EffectPass pass in horizonTextureEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(horizonPanoramaBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        horizonPanoramaBatch.PrimitiveCount);
                }
            }
        }

        private void DrawColourBatches(
            IEnumerable<TrackColourBatch> batches,
            Vector3 cameraPosition,
            BoundingFrustum viewFrustum,
            HashSet<int>? visibleBlockIdentifiers)
        {
            foreach (TrackColourBatch currentColourBatch in batches)
            {
                if (!IsVisible(
                        currentColourBatch.BlockIdentifier,
                        currentColourBatch.DetailLevel,
                        currentColourBatch.ResolutionCentre,
                        currentColourBatch.Bounds,
                        cameraPosition,
                        viewFrustum,
                        visibleBlockIdentifiers))
                {
                    continue;
                }

                foreach (EffectPass pass in colourEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(currentColourBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        currentColourBatch.PrimitiveCount);
                }
            }
        }

        private void DrawTextureBatches(
            Vector3 cameraPosition,
            BoundingFrustum viewFrustum,
            HashSet<int>? visibleBlockIdentifiers)
        {
            foreach (TrackTextureBatch textureBatch in textureBatches)
            {
                if (!IsVisible(
                        textureBatch.BlockIdentifier,
                        textureBatch.DetailLevel,
                        textureBatch.ResolutionCentre,
                        textureBatch.Bounds,
                        cameraPosition,
                        viewFrustum,
                        visibleBlockIdentifiers))
                {
                    continue;
                }

                textureEffect.Texture = textureResources[textureBatch.TextureIdentifier].Texture;

                foreach (EffectPass pass in textureEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    graphicsDevice.SetVertexBuffer(textureBatch.VertexBuffer);
                    graphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        textureBatch.PrimitiveCount);
                }
            }
        }

        private static bool IsVisible(
            int blockIdentifier,
            TrackGeometryDetailLevel detailLevel,
            Vector3 resolutionCentre,
            BoundingSphere bounds,
            Vector3 cameraPosition,
            BoundingFrustum viewFrustum,
            HashSet<int>? visibleBlockIdentifiers)
        {
            if (!TrackBlockVisibilitySelector.IsVisible(
                    blockIdentifier,
                    visibleBlockIdentifiers) ||
                viewFrustum.Contains(bounds) == ContainmentType.Disjoint)
            {
                return false;
            }

            if (detailLevel == TrackGeometryDetailLevel.Unrestricted)
            {
                return true;
            }

            float distanceX = cameraPosition.X - resolutionCentre.X;
            float distanceZ = cameraPosition.Z - resolutionCentre.Z;
            float horizontalDistanceSquared =
                distanceX * distanceX +
                distanceZ * distanceZ;

            return detailLevel == TrackLevelOfDetailSelector.Select(
                horizontalDistanceSquared);
        }

        private void LoadHorizon(TrackHorizon? horizon)
        {
            if (horizon is null)
            {
                return;
            }

            VertexPositionColor[] ringVertices = TrackHorizonVertexBuilder
                .BuildRing(horizon)
                .ToArray();

            if (ringVertices.Length > 0)
            {
                horizonRingBatch = new TrackColourBatch(
                    graphicsDevice,
                    ringVertices);
            }

            TrackColour sourceSkyColour = horizon.SkyColour;
            Color skyColour = new(
                sourceSkyColour.Red,
                sourceSkyColour.Green,
                sourceSkyColour.Blue);
            VertexPositionColorTexture[] domeVertices = TrackHorizonVertexBuilder
                .BuildDome(horizon)
                .ToArray();

            if (domeVertices.Length > 0)
            {
                horizonDomeBatch = new TrackColourBatch(
                    graphicsDevice,
                    domeVertices
                        .Select(vertex => new VertexPositionColor(
                            vertex.Position,
                            skyColour))
                        .ToArray());
            }

            if (horizon.PanoramaTexture is null)
            {
                return;
            }

            horizonTextureResource = new TrackTextureResource(
                graphicsDevice,
                horizon.PanoramaTexture);
            VertexPositionColorTexture[] panoramaVertices = TrackHorizonVertexBuilder
                .BuildPanorama(horizon)
                .ToArray();

            if (panoramaVertices.Length > 0)
            {
                horizonPanoramaBatch = new TrackTextureBatch(
                    graphicsDevice,
                    horizon.PanoramaTexture.Identifier,
                    panoramaVertices);
            }
        }

        private Vector3 ResolveBlockCentre(int blockIdentifier)
        {
            if (blockCentres.TryGetValue(
                    blockIdentifier,
                    out Vector3 blockCentre))
            {
                return blockCentre;
            }

            return Vector3.Zero;
        }

        private HashSet<int>? ResolveVisibleBlockIdentifiers(Vector3 cameraPosition)
        {
            int nearestBlockIdentifier = TrackBlockVisibilitySelector.SelectNearestBlock(
                blockCentres,
                cameraPosition);

            if (visibleBlockIdentifiersByBlock.TryGetValue(
                    nearestBlockIdentifier,
                    out HashSet<int>? visibleBlockIdentifiers))
            {
                return visibleBlockIdentifiers;
            }

            return null;
        }

        private static Vector3 ToVector3(TrackPoint point)
            => new((float)point.X, (float)point.Y, (float)point.Z);

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(TrackRenderer));
            }
        }
    }
}
