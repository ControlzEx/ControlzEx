// reference from https://github.com/sourcechord/FluentWPF/blob/master/FluentWPF/Utility/VersionInfo.cs
// LICENSE: https://github.com/sourcechord/FluentWPF/blob/master/LICENSE

namespace ControlzEx.Showcase
{
    using System;

    public readonly struct SystemVersionInfo
    {
        public static SystemVersionInfo Windows10 => new(10, 0, 10240);

        public static SystemVersionInfo Windows10_1809 => new(10, 0, 17763);

        public static SystemVersionInfo Windows10_1903 => new(10, 0, 18362);

        public SystemVersionInfo(int major, int minor, int build, int revision = 0)
        {
            this.Major = major;
            this.Minor = minor;
            this.Build = build;
            this.Revision = revision;
        }

        public SystemVersionInfo(Version version)
        {
            this.Major = version.Major;
            this.Minor = version.Minor;
            this.Build = version.Build;
            this.Revision = version.Revision;
        }

        public int Major { get; }

        public int Minor { get; }

        public int Build { get; }

        public int Revision { get; }

        public bool Equals(SystemVersionInfo other) => this.Major == other.Major && this.Minor == other.Minor && this.Build == other.Build && this.Revision == other.Revision;

        public override bool Equals(object obj) => obj is SystemVersionInfo other && this.Equals(other);

        public override int GetHashCode() => this.Major.GetHashCode() ^ this.Minor.GetHashCode() ^ this.Build.GetHashCode() ^ this.Revision.GetHashCode();

        public static bool operator ==(SystemVersionInfo left, SystemVersionInfo right) => left.Equals(right);

        public static bool operator !=(SystemVersionInfo left, SystemVersionInfo right) => !(left == right);

        public int CompareTo(SystemVersionInfo other)
        {
            if (this.Major != other.Major)
            {
                return this.Major.CompareTo(other.Major);
            }

            if (this.Minor != other.Minor)
            {
                return this.Minor.CompareTo(other.Minor);
            }

            if (this.Build != other.Build)
            {
                return this.Build.CompareTo(other.Build);
            }

            if (this.Revision != other.Revision)
            {
                return this.Revision.CompareTo(other.Revision);
            }

            return 0;
        }

        public int CompareTo(object obj)
        {
            if (obj is not SystemVersionInfo other)
            {
                return 0;
            }

            return this.CompareTo(other);
        }

        public static bool operator <(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) < 0;

        public static bool operator <=(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) <= 0;

        public static bool operator >(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) > 0;

        public static bool operator >=(SystemVersionInfo left, SystemVersionInfo right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{this.Major}.{this.Minor}.{this.Build}.{this.Revision}";
    }
}
