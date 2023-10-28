namespace SlyvekLauncherCore
{
    public class Version
    {
        public int Major, Minor, Release, Fix;

        public Version(string v)
        {
            Major = int.Parse(v.Split('.')[0]);
            Minor = int.Parse(v.Split('.')[1]);
            Release = int.Parse(v.Split('.')[2]);
            Fix = int.Parse(v.Split('.')[3]);
        }

        public override string ToString()
        {
            return Major + "." + Minor + "." + Release + "." + Fix;
        }

        public bool isHigher(Version v)
        {
            bool can = true;

            if (Major > v.Major)
                return true && can;
            else if (Major < v.Major)
                can = false;

            if (Minor > v.Minor)
                return true && can;
            else if (Minor < v.Minor)
                can = false;

            if (Release > v.Release)
                return true && can;
            else if (Release < v.Release)
                can = false;

            if (Fix > v.Fix)
                return true && can;
            else if (Fix < v.Fix)
                can = false;

            return false;
        }
    }
}