using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DogWalkAPI.Models
{
	public class Walker
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int NeighborhoodId { get; set; }
		public List<Walks> Walks { get; set; }
	}
}
