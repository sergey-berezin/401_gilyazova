using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Server
{
    public class ImageInfo
    {
        public int ImageInfoId { get; set; }
        public string filename { get; set; }
        public int hash { get; set; }
        public ImageValue value { get; set; }
        public ICollection<Emotion_> Emotions { get; set; }
        public ImageInfo()
        {
            filename = ImageInfoId.ToString();

            Emotions = new List<Emotion_>();
        }
    }

    public class ImageValue
    {
        [Key]
        public int ImageInfoId { get; set; }
        public byte[] data { get; set; }
        public ImageInfo image { get; set; }
    }

    public class Emotion_
    {
        public int Emotion_Id { get; set; }
        public float value { get; set; }
        public string name { get; set; }
        public int ImageInfoId { get; set; }
        public ImageInfo image { get; set; }
    }

    public class ApplicationContext : DbContext
    {
        public DbSet<ImageInfo> images { get; set; }
        public DbSet<Emotion_> Emotions { get; set; }
        public DbSet<ImageValue> values { get; set; }

        //public ApplicationContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlite("Data Source=images.db");
    }
}