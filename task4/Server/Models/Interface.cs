using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using EmotionFerPlus;
using System.Collections;
using System.IO;
using SixLabors.ImageSharp.Advanced;
using System.Runtime.InteropServices;



namespace Server
{
    public interface IImagesDb
    {
        Task<bool> PostImage(byte[] img, CancellationTokenSource ctn);
        IEnumerable<int> GetAllImagesId();
        ImageInfo? GetImageById(int id);
        Task<bool> DeleteAllImages();
    }

    public class InMemoryDb : IImagesDb
    {
        private Emotion EmotionFerPlusModel = new Emotion();
        private CancellationTokenSource cts = new CancellationTokenSource();
        public async Task<bool> PostImage(byte[] image, CancellationTokenSource ctn)
        {
            var myStream = new MemoryStream(image);
            using Image<Rgb24> image_rgb = Image.Load<Rgb24>(myStream);
            int hash = image_rgb.GetHashCode();


            using (var db = new ApplicationContext())
            {
                var query = db.images.Where(x => x.hash == hash).Include(item => item.value);
                var item = query.Where(x => Enumerable.SequenceEqual(x.value.data, image))
                            .Include(x => x.Emotions)
                            .FirstOrDefault();
                if ((item != null) && (item.hash == hash))
                    return false;
                else
                {

                    var result = await EmotionFerPlusModel.ProcessAsync(image_rgb, ctn.Token);
                    var tmpImage = new ImageInfo();
                    tmpImage.value = new ImageValue() { data = image, image = tmpImage };
                    tmpImage.hash = hash;

                    foreach (var elem in result)
                    {
                        tmpImage.Emotions.Add(new Emotion_() { name = elem.Key, value = elem.Value, image = tmpImage });
                    }


                    db.images.Add(tmpImage);
                    db.SaveChanges();

                    return true;
                }
            }
        }
        public IEnumerable<int> GetAllImagesId()
        {
            using (var db = new ApplicationContext())
            {
                return db.images.Select(x => x.ImageInfoId).ToList();
            }   
        }

        public ImageInfo? GetImageById(int id)
        {
            using (var db = new ApplicationContext())
            {
                return db.images.Where(x => x.ImageInfoId == id)
                                .Include(x => x.value)
                                .Include(x => x.Emotions).FirstOrDefault();
            }
        }
        public async Task<bool> DeleteAllImages()
        {
            try
            {
                using (var db = new ApplicationContext())
                {
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM [images]");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}