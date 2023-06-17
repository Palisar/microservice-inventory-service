using System.Threading.Tasks;
using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers
{
    public class CatalogItemUpdatedConsumer : IConsumer<CatalogIItemCreated>
    {
        public readonly IRepository<CatalogItem> _repository;

        public CatalogItemUpdatedConsumer(IRepository<CatalogItem> repository)
        {
            _repository = repository;
        }


        public async Task Consume(ConsumeContext<CatalogIItemCreated> context)
        {
            var message = context.Message;

            var item = await _repository.GetAsync(message.Id);

            if (item == null)
            {
                item = new CatalogItem()
                {
                    Id = message.Id,
                    Name = message.Name,
                    Description = message.Description
                };

                await _repository.CreateAsync(item);
            }
            else
            {
                item.Name = message.Name;
                item.Description = message.Description;
                await _repository.UpdateAsync(item);
            }

        }
    }
}
