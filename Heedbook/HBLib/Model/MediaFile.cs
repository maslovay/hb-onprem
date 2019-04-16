using System;

namespace HBLib.Model
{
    public class MediaFile
    {
        private String _name;

        public MediaFile(String extension)
        {
            Extension = extension;
        }

        public String Name
        {
            get => _name;
            set => _name = GetNameWithExtension(value);
        }

        public Byte[] Content { get; set; }
        public String Extension { get; }

        private String GetNameWithExtension(String value)
        {
            if (String.IsNullOrWhiteSpace(value) || String.IsNullOrWhiteSpace(Extension))
                return value;

            return value.EndsWith(Extension, StringComparison.CurrentCultureIgnoreCase)
                ? value
                : $"{value}.{Extension}";
        }
    }
}