using System;
using System.Collections.Generic;
using System.Text;

namespace LifeApp.SDK.Models
{
    public partial class Movie
    {
        public List<string> Genres { get; set; } = [];

        public List<string> Providers { get; set; } = [];
    }
}
