using System;
using System.IO;

namespace OpenSpeed.Classic.Cars.NeedForSpeed2
{
    public static class NeedForSpeed2CarCatalogue
    {
        private static readonly string[] DisplayNames =
        [
            "McLaren F1", "Ferrari F50", "Ferrari F355", "Ford GT90",
            "Ford Indigo", "Ford Mustang Mach III", "Jaguar XJ220", "Lotus GT1",
            "Lotus Esprit V8", "Italdesign Nazca C2", "Italdesign Cala",
            "Isdera Commendatore", "Bonus Chevrolet", "Bonus Daytona", "Bonus Future"
        ];

        private static readonly string[] ResourceNames =
        [
            "MCF1", "FF50", "F355", "GT90", "IDGO", "MACH", "JAGR", "LGT1",
            "ESPR", "NAZC", "CALA", "ISDE", "CHEV", "DAYT", "FUTR"
        ];

        private static string CarDataArchiveRelativePath
            => Path.Combine("gamedata", "sim", "cardata", "cardata.viv");

        private static string CarModelDirectory
            => Path.Combine("gamedata", "carmodel", "pc");

        public static CarIdentifier ParseIdentifier(string identifier)
        {
            bool identifierWasParsed = Enum.TryParse(
                identifier,
                true,
                out CarIdentifier parsedIdentifier);

            if (!identifierWasParsed || !Enum.IsDefined(parsedIdentifier))
            {
                throw new ArgumentException(
                    $"The NFS II car identifier '{identifier}' is not supported.",
                    nameof(identifier));
            }

            return parsedIdentifier;
        }

        public static string GetArchiveRelativePath() => CarDataArchiveRelativePath;

        public static string GetDisplayName(CarIdentifier identifier)
            => DisplayNames[(int)identifier];

        public static string GetGeometryMemberName(CarIdentifier identifier)
            => $"{GetResourceName(identifier)}.geo";

        public static string GetTextureRelativePath(CarIdentifier identifier)
            => Path.Combine(CarModelDirectory, $"{GetResourceName(identifier)}a.qfs");

        private static string GetResourceName(CarIdentifier identifier)
        {
            int identifierIndex = (int)identifier;

            if (identifierIndex < 0 || identifierIndex >= ResourceNames.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(identifier),
                    identifier,
                    "The car identifier is outside the original NFS II car table.");
            }

            return ResourceNames[identifierIndex];
        }
    }
}