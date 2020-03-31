using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DogWalkAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DogWalkAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OwnerController : Controller
    {
        private readonly IConfiguration _config;

        public OwnerController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string include)
        {
            return include == "neighborhood" ? Ok(GetOwnersWithNeighborhood()) : Ok(GetOwners());
        }


        [HttpGet("{id}", Name = "GetOwners")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone
                        FROM Owner o
                        WHERE o.Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Owner owner = null;


                    while (reader.Read())
                    {
                        if (owner == null)
                        {
                            owner = new Owner
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                Phone = reader.GetString(reader.GetOrdinal("Phone")),
                                NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Dogs = new List<Dog>()
                            };
                        }

                        owner.Dogs.Add(new Dog()
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("DogId")),
                            Name = reader.GetString(reader.GetOrdinal("DogName")),
                            Breed = reader.GetString(reader.GetOrdinal("Breed")),
                            OwnerId = reader.GetInt32(reader.GetOrdinal("OwnerId"))
                        });
                    }

                    reader.Close();

                    return Ok(owner);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Owner owner)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Owner (Name, Address, NeighborhoodId, Phone)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @address, @neighborhoodId, @phone)";
                    cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                    cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                    cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                    cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));



                    int newId = (int)cmd.ExecuteScalar();
                    owner.Id = newId;
                    return CreatedAtRoute("GetOwners", new { id = newId }, owner);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Owner owner)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Owner
                                            SET Name = @name,
                                                Address = @address,
                                                NeighborhoodId = @neighborhoodId,
                                                Phone = @phone
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", owner.Name));
                        cmd.Parameters.Add(new SqlParameter("@address", owner.Address));
                        cmd.Parameters.Add(new SqlParameter("@neighborhoodId", owner.NeighborhoodId));
                        cmd.Parameters.Add(new SqlParameter("@phone", owner.Phone));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!OwnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private List<Owner> GetOwners()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone
                        FROM Owner o";

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Owner> owners = new List<Owner>();

                    Owner owner = null;

                    while (reader.Read())
                    {
                        owner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId"))
                        };

                        owners.Add(owner);
                    }
                    reader.Close();

                    return owners;
                }
            }
        }

        private List<Owner> GetOwnersWithNeighborhood()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT o.Id, o.Name, o.Address, o.NeighborhoodId, o.Phone, n.Id as NeighborhoodId, n.Name as NeighborhoodName
                        FROM Owner o
                        JOIN Neighborhood n ON n.Id = o.NeighborhoodId";

                    var reader = cmd.ExecuteReader();
                    var owners = new List<Owner>();

                    Owner owner = null;

                    while (reader.Read())
                    {
                        owner = new Owner
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Address = reader.GetString(reader.GetOrdinal("Address")),
                            Phone = reader.GetString(reader.GetOrdinal("Phone")),
                            NeighborhoodId = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                            Neighborhood = new Neighborhood()
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("NeighborhoodId")),
                                Name = reader.GetString(reader.GetOrdinal("NeighborhoodName")),
                            }
                        };

                        owners.Add(owner);
                    }
                    reader.Close();

                    return owners;
                }
            }
        }

        private bool OwnerExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                    SELECT Id, Name, Address, NeighborhoodId, Phone
                    FROM Owner
                    WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}

