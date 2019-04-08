using System;

namespace HBLib.Model
{
    public class MediaFile
    {
        private string _name;
        public string Name
        {
            get => _name;
            set => _name = GetNameWithExtension(value);
        }

        public byte[] Content { get; set; }
        public string Extension { get; }

        public MediaFile(string extension)
        {
            Extension = extension;
        }

        private string GetNameWithExtension(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(Extension))
                return value;

            return value.EndsWith(Extension, StringComparison.CurrentCultureIgnoreCase)
                ? value
                : $"{value}.{Extension}";
        }
    }
}