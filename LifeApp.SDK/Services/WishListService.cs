using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Models;
using LifeApp.SDK.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LifeApp.SDK.Services
{
    public class WishListService(IUnitOfWork unitOfWork, IOperationResult result, ILogger<WishListService> logger) : IWishListService
    {
        public IUnitOfWork UnitOfWork { get; set; } = unitOfWork;
        public IOperationResult Result { get; set; } = result;

        private readonly ILogger _logger = logger;

        private NPoco.IDatabase Db => ((NPocoUnitOfWork)UnitOfWork).Db;
 
        public List<WishList> GetAllWishlists()
        {
            Result.Reset();

            List<WishList> wishlists;

            try
            {
                wishlists = Db.Fetch<WishList>(
                    @"
                    SELECT *
                    FROM WishLists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching all wishlists");
               
                Result.GetException(ex);
                
                throw;
            }
            _logger.LogInformation($"Fetched all wishlists.");
            
            return wishlists;
        }

        public List<WishList> GetAllActiveWishlists()
        {
            Result.Reset();

            List<WishList> wishlists;

            try
            {
                wishlists = Db.Fetch<WishList>(
                    @"
                    SELECT *
                    FROM WishLists
                    WHERE IsEnabled = 1");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching all active wishlists");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Fetched all active wishlists.");

            return wishlists;
        }

        public IOperationResult InsertWishlist(WishList wishlist)
        {
            Result.Reset();

            try
            {
                UnitOfWork.BeginTransaction();

                Db.Insert(wishlist);

                UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error inserting wishlist for {wishlist.WishListName}");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Inserted wishlist for {wishlist.WishListName}");

            return Result;
        }

        public IOperationResult UpdateWishlist(WishList wishlist)
        {
            Result.Reset();

            try
            {
                UnitOfWork.BeginTransaction();

                Db.Update(wishlist);

                UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error updating wishlist for {wishlist.WishListName}");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Updated wishlist for {wishlist.WishListName}");

            return Result;
        }
    }
}
