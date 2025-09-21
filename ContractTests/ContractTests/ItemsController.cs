using ContractTests.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ContractTests.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        // In-memory data store with sample data
        private static readonly List<Item> Items = new()
        {
            new Item { Id = 1, Name = "Sample Item 1" }
        };

        // GET: api/items
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Item>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Item>> Get()
        {
            return Ok(Items);
        }

        //ProducesResponseType This makes your API endpoints clearer and better documented for consumers and tools.
        //API Documentation
        //Clarity
        //Tooling Support
        [HttpPost]
        [ProducesResponseType(typeof(Item), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public ActionResult<Item> Post([FromBody] Item item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
                return BadRequest("Item name cannot be empty.");

            item.Id = Items.Any() ? Items.Max(i => i.Id) + 1 : 1;
            Items.Add(item);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }
    }
}
