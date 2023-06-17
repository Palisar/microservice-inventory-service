using System.Threading.Tasks;
using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers
{
    public class CatalogItemDeletedConsumer : IConsumer<CatalogIItemCreated>
    {
        public readonly IRepository<CatalogItem> _repository;
            
        public CatalogItemDeletedConsumer(IRepository<CatalogItem> repository)
        {
            _repository = repository;
        }


        public async Task Consume(ConsumeContext<CatalogIItemCreated> context)
        {
            var message = context.Message;

            var item = await _repository.GetAsync(message.Id);

            if (item == null)
            {
                return;
            }

            await _repository.RemoveAsync(message.Id);


        }
    }
}
