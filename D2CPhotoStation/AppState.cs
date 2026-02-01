namespace D2CPhotoStation
{
    public static class AppState
    {
        // Default destination base path (can be changed in Settings)
        public static string BaseSavePath = @"C:\Users\sumit\OneDrive\Desktop\D2C\";

        // Selected camera moniker string (AForge identifier)
        public static string SelectedCameraMoniker = null;

        // Settings login (hardcoded)
        public static readonly string SettingsUsername = "ctdi";
        public static readonly string SettingsPassword = "Ctdi123@";
    }
}
