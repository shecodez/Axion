using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using SharpDX;

namespace Axion.objects
{
    class Device
    {
        private byte[] backBuffer;
        private WriteableBitmap bmp;

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            // The back buffer size is equal to the number of pixels to draw
            // on screen (width*height) * 4 (R,G,B & Alpha values).
            backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
        }

        // This method is called to clear the back buffer with a specific color
        public void Clear(byte r, byte g, byte b, byte a)
        {
            for (var index = 0; index < backBuffer.Length; index += 4)
            {
                // BGRA is used by Windows
                backBuffer[index]     = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }
        }

        // Once everything is clear we flush the back buffer into the front buffer.
        public void Present()
        {
            using (var stream = bmp.PixelBuffer.AsStream())
            {
                // Writing our byte[] back buffer into our WriteableBitmap Stream
                stream.Write(backBuffer, 0, backBuffer.Length);
            }
            // Request redraw of the entire bitmap
            bmp.Invalidate();
        }

        // called to put a pixel on screen at specific X,Y coordinates
        public void PutPixel(int x, int y, Color4 color)
        {
            // As we have a 1-D Array for our back buffer
            // we need to know the equivalent cell in 1-D based
            // on the 2D coordinates on screen.
            var index = (x + y * bmp.PixelWidth) * 4;

            backBuffer[index]     = (byte)(color.Blue * 255);
            backBuffer[index + 1] = (byte)(color.Green * 255);
            backBuffer[index + 2] = (byte)(color.Red * 255);
            backBuffer[index + 3] = (byte)(color.Alpha * 255);

        }

        // Project takes some 3D coordinates and transforms them
        // into 2D coordinates using the transformation matrix.
        public Vector2 Project(Vector3 coord, Matrix transMat)
        {
            // transforming the coordinates
            var point = Vector3.TransformCoordinate(coord, transMat);

            // The transformed coordinates will be based on the coordinate system
            // starting at the center of the screen. However drawing on the screen
            // normally starts from the top left. So we need to transform them again
            // to have x:0, y:0 start at the top left.
            var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
            var y = point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
            
            return (new Vector2(x, y));
        }

        // DrawPoint calls PutPixel but does the clipping op first.
        public void DrawPoint(Vector2 point)
        {
            // Clipping what's visible on screen
            if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
            {
                // Draw a yellow point
                PutPixel((int)point.X, (int)point.Y, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
            }
        }

        // The main method of the engine that re-computes each vertex projection during each frame.
        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix = Matrix.PerspectiveFovRH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

            foreach(Mesh mesh in meshes)
            {
                // Apply rotation before translation
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z)
                                * Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                foreach (var vertex in mesh.Vertices)
                {
                    // First project the 3D coordinates in to the 2D space
                    var point = Project(vertex, transformMatrix);
                    // Then draw it to screen
                    DrawPoint(point);
                }
            }
        }
    }
}
