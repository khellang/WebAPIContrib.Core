using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using WebApiContrib.Core.Samples.Model;

namespace WebApiContrib.Core.Samples.Controllers
{
    [Route("api/versioned")]
    public class VersionedController : Controller
    {
        [HttpGet("{id}")]
        public PersonModel Get([FromRoute] int id)
        {
            return new PersonModel("Person", "#1", age: 12);
        }

        [HttpGet]
        public IEnumerable<PersonModel> GetAll()
        {
            return new[]
            {
                new PersonModel("Person", "#1", age: 12),
                new PersonModel("Person", "#2", age: 26),
            };
        }
    }
}