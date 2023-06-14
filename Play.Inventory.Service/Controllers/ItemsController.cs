using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Play.Inventory.Service.Clients;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _itemsRepository;
        private readonly CatalogClient _catalogClient;
        public ItemsController(IRepository<InventoryItem> itemsRepository, CatalogClient catalogClient)
        {
            _itemsRepository = itemsRepository;
            _catalogClient = catalogClient;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return BadRequest();

            var catalogItems = await _catalogClient.GetCatalogItemsAsync();

            var inventoryItemEntities = await _itemsRepository.GetAllAsync(item => item.UserId == userId);

            var inventoryItemDtos = inventoryItemEntities.Select(item =>
            {
                var catalogItem = catalogItems.Single(catalogItem => catalogItem.Id == item.CatalogItemId);
                return item.AsDto(catalogItem.Name, catalogItem.Description);
            });

            return Ok(inventoryItemDtos);
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await _itemsRepository.GetAsync(item =>
                item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);
            if (inventoryItem is null)
            {
                inventoryItem = new InventoryItem
                {
                    Id = grantItemsDto.CatalogItemId,
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    UserId = grantItemsDto.UserId,
                    Quantity = grantItemsDto.Quantity,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _itemsRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await _itemsRepository.UpdateAsync(inventoryItem);
            }

            return Ok();
        }

      
    }
}
