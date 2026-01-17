using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace SatoSim.Core.Utils
{
    public class AsyncTexture2D : IDisposable
    {
        public enum JacketState
        {
            LOADING = 0x0,
            READY = 0x1,
            NULL_OR_FAIL = 0xF
        }
    
        public Texture2D Texture { get; private set; }
        public JacketState State { get; private set; }

        private readonly Task _loadingRoutine;
        
        
        
        public AsyncTexture2D(string path)
        {
            State = JacketState.LOADING;

            _loadingRoutine = Task.Run(() =>
            {
                try
                {
                    if (File.Exists(path))
                    {
                        using var stream = new FileStream
                        (
                            path,
                            FileMode.Open, FileAccess.Read, FileShare.Read,
                            (int)new FileInfo(path).Length,
                            true
                        );
                    
                        Texture = Texture2D.FromStream(Game1.Graphics.GraphicsDevice, stream);
                        State = JacketState.READY;
                    }
                    else
                    {
                        Console.WriteLine("The texture file is not found.");
                        State = JacketState.NULL_OR_FAIL;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("There was an error reading texture file: {0}", e);
                    State = JacketState.NULL_OR_FAIL;
                }
            });
        }
        
        ~AsyncTexture2D()
        {
            Dispose();
        }

        public void Dispose()
        {
            _loadingRoutine?.Dispose();
            Texture?.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}