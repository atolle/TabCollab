using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TabRepository.Data;

namespace TabRepository.Helpers
{
    public class UserAuthenticator
    {
        private ApplicationDbContext _context;

        public UserAuthenticator(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool CheckUserReadAccess(Item item, int id, string userId)
        {
            switch (item)
            {
                case Item.Project:
                    return true;

                case Item.Album:
                    return true;

                case Item.Tab:
                    return true;

                default: return false;
            }
        }

        public bool CheckUserWriteAccess(Item item, int id, string userId)
        {
            switch (item)
            {
                case Item.Project:
                    return true;

                case Item.Album:
                    return true;

                case Item.Tab:
                    return true;

                default: return false;
            }
        }
    }

    public enum Item
    {
        Project,
        Album,
        Tab,
        TabVersion
    }
}
