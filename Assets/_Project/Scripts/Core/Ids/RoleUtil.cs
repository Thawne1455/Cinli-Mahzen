namespace CinliMahzen.Core
{
    public static class RoleUtil
    {
        public static Team TeamOf(Role r)
        {
            switch (r)
            {
                case Role.Human:
                case Role.GoodJinn:
                    return Team.Seekers;
                case Role.EvilJinn:
                    return Team.Jinns;
                default:
                    return Team.None;
            }
        }
    }
}
