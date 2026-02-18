using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeApp.SDK.Interfaces.Services
{
    public interface IWishListService
    {
        public List<WishList> GetAllWishlists();

        public List<WishList> GetAllActiveWishlists();

        public IOperationResult InsertWishlist(WishList wishlist);

        public IOperationResult UpdateWishlist(WishList wishlist);
    }
}
