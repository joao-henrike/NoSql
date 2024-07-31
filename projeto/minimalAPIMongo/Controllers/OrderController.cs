using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using minimalAPIMongo.Domains;
using minimalAPIMongo.Service;
using minimalAPIMongo.ViewModels;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace minimalAPIMongo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private readonly IMongoCollection<Order>? _order;
        private readonly IMongoCollection<Client>? _client;
        private readonly IMongoCollection<Product>? _product;

        public OrderController(MongoDbService mongoDbService)
        {
            _order = mongoDbService.GetDatabase?.GetCollection<Order>("order");
            _client = mongoDbService.GetDatabase?.GetCollection<Client>("client");
            _product = mongoDbService.GetDatabase?.GetCollection<Product>("product");
        }

        [HttpPost]
        public async Task<ActionResult<Order>> Create(OrderViewModel orderViewModel)
        {
            try
            {
                Order order = new Order();

                order.Id = orderViewModel.Id;
                order.Date = orderViewModel.Date;
                order.Status = orderViewModel.Status;
                order.ProductId = orderViewModel.ProductId;
                order.ClientId = orderViewModel.ClientId;

                var client = await _client.Find(x => x.Id == order.ClientId).FirstOrDefaultAsync();

                if (client == null)
                {
                    return NotFound("Cliente não existe");
                }
                order.Client = client;

                await _order.InsertOneAsync(order);

                return StatusCode(201, order);
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Erro interno: {e.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<Order>>> Get()
        {
            try
            {
                var orders = await _order.Find(FilterDefinition<Order>.Empty).ToListAsync();

                foreach (var order in orders)
                {
                    if (order.ProductId != null)
                    {
                        var filter = Builders<Product>.Filter.In(p => p.Id, order.ProductId);

                        order.Products = await _product.Find(filter).ToListAsync();
                    }

                    if (order.ClientId != null)
                    {
                        order.Client = await _client.Find(x => x.Id == order.ClientId).FirstOrDefaultAsync();
                    }
                }
                return Ok(orders);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetElementById(string id)
        {
            try
            {
                var order = await _order.Find(x => x.Id == id).FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound($"Pedido com ID {id} não encontrado.");
                }

                if (order.ProductId != null)
                {
                    var filter = Builders<Product>.Filter.In(p => p.Id, order.ProductId);
                    order.Products = await _product.Find(filter).ToListAsync();
                }

                if (order.ClientId != null)
                {
                    order.Client = await _client.Find(x => x.Id == order.ClientId).FirstOrDefaultAsync();
                }

                return Ok(order);
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleteResult = await _order.DeleteOneAsync(x => x.Id == id);
                if (deleteResult.DeletedCount > 0)
                {
                    return Ok($"Pedido com ID {id} removido com sucesso.");
                }
                else
                {
                    return NotFound($"Pedido com ID {id} não encontrado.");
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Erro interno: {e.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, OrderViewModel orderViewModel)
        {
            try
            {
                var updateDefinition = Builders<Order>.Update.Set(o => o.Date, orderViewModel.Date)
                    .Set(o => o.Status, orderViewModel.Status)
                    .Set(o => o.ProductId, orderViewModel.ProductId)
                    .Set(o => o.ClientId, orderViewModel.ClientId);

                var updateResult = await _order.UpdateOneAsync(x => x.Id == id, updateDefinition);

                if (updateResult.ModifiedCount > 0)
                {
                    return Ok($"Pedido com ID {id} atualizado com sucesso.");
                }
                else
                {
                    return NotFound($"Pedido com ID {id} não encontrado.");
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, $"Erro interno: {e.Message}");
            }
        }
    }
}
