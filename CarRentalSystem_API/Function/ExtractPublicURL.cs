namespace CarRentalSystem_API.Function
{
    public class ExtractPublicURL
    {
        public static string ExtractPublicIDFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            string folderName = "CarRentalSystem/Banners/";
            int startIndex = url.IndexOf(folderName);
            if (startIndex != -1)
            {
                string publicIdWithExtension = url.Substring(startIndex);

                int lastDotIndex = publicIdWithExtension.LastIndexOf('.');
                if (lastDotIndex != -1)
                {
                    return publicIdWithExtension.Substring(0, lastDotIndex);
                }
                return publicIdWithExtension;
            }
            return string.Empty;
        }
    }
}
