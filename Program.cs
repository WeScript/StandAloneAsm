using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics;
using SharpDX.Direct3D9;
using SharpDX.XInput;
using WeScriptWrapper;
using System.Resources;
using StandAloneAsm.Properties;
using System.Drawing;
//using WeScript.SDK.Rendering;

namespace StandAloneAsm
{
    class Program
    {

        //public static Texture UltimateCircle;
        //public static string uniquekey;
        //public static readonly TextureLoader TextureLoader = new TextureLoader();

        //public Program()
        //{
        //    //System.Resources.ResourceManager res_mng = new System.Resources.ResourceManager(typeof(Resources));

        //    var bitmap = (Bitmap)Resources.ResourceManager.GetObject("UltCircle");





        //    //var cutBitmap = BitmapHelper.ResizeImage(bitmap, new Size(24, 24));

        //    //UltimateCircle = TrackersCommon.TextureLoader.Load(cutBitmap, out _);
        //}

        public static byte[] imageBytesPLS;
        public static uint imageIndex;
        public static IntPtr procHnd = IntPtr.Zero;
        public static uint PROCESS_ALL_ACCESS = 0x1FFFFF;
        public static bool is64bit = false;
        public static bool drawyourstuff = false;
        public static bool isGameOnTop = false;
        public static Vector2 wndMargins = new Vector2(0, 0);
        public static Vector2 wndSize = new Vector2(0, 0);
        public static IntPtr client_panorama = IntPtr.Zero;
        public static IntPtr client_panorama_size = IntPtr.Zero;
        public static IntPtr dwViewMatrix_Offs = IntPtr.Zero;
        public static IntPtr dwEntityList_Offs = IntPtr.Zero;

        public static bool WorldToScreen(Vector3 pos, out Vector2 screen, Matrix matrix, Vector2 wndMargins, Vector2 wndSize) // 3D to 2D
        {
            //Matrix-vector Product, multiplying world(eye) coordinates by projection matrix = clipCoords
            Vector4 clipCoords = new Vector4();
            screen = new Vector2(0, 0);
            clipCoords.X = pos.X * matrix[0] + pos.Y * matrix[1] + pos.Z * matrix[2] + matrix[3];
            clipCoords.Y = pos.X * matrix[4] + pos.Y * matrix[5] + pos.Z * matrix[6] + matrix[7];
            clipCoords.W = pos.X * matrix[12] + pos.Y * matrix[13] + pos.Z * matrix[14] + matrix[15];

            if (clipCoords.W < 0.1f) return false;

            //perspective division, dividing by clip.W = Normalized Device Coordinates
            Vector3 NDC = new Vector3();
            NDC.X = clipCoords.X / clipCoords.W;
            NDC.Y = clipCoords.Y / clipCoords.W;
            //NDC.Z = clipCoords.Z / clipCoords.W;

            //Transform to window coordinates and addup window margin
            screen.X = (wndSize.X / 2 * NDC.X) + (NDC.X + wndSize.X / 2) + wndMargins.X;
            screen.Y = -(wndSize.Y / 2 * NDC.Y) + (NDC.Y + wndSize.Y / 2) + wndMargins.Y;

            //stop drawing outside the game window (in case it's smaller than desktop res)
            if (screen.X < wndMargins.X) return false;
            if (screen.X > wndMargins.X + wndSize.X) return false;
            if (screen.Y < wndMargins.Y) return false;
            if (screen.Y > wndMargins.Y + wndSize.Y) return false;

            return true;
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        static void Main(string[] args)
        {
            var bitmap = (Bitmap)Resources.ResourceManager.GetObject("UltCircle");
            imageBytesPLS = ImageToByte(bitmap);
            imageIndex = Renderer.CreateSprite(imageBytesPLS, imageBytesPLS.Length);
            var kotdodi = Renderer.GetSpriteDimensions(imageBytesPLS, imageBytesPLS.Length);

            //UltimateCircle = TextureLoader.Load(bitmap, out uniquekey);
            Console.WriteLine("Hello from CSGO.Assembly!");
            Console.WriteLine($"image.x {kotdodi.X} image.y {kotdodi.Y}");
            Renderer.OnRenderer += OnRenderer;
            Memory.OnTick += OnTick;
            Input.OnInput += OnInput;
   

        }



        private static void OnTick(int counter, EventArgs args)
        {
            if (procHnd == IntPtr.Zero)
            {
                var wndHnd = Memory.FindWindowClassName("Valve001");
                if (wndHnd != IntPtr.Zero)
                {
                    var csgoPID = Memory.GetPIDFromHWND(wndHnd);
                    if (csgoPID > 0)
                    {
                        procHnd = Memory.OpenProcess(PROCESS_ALL_ACCESS, csgoPID);
                        if (procHnd != IntPtr.Zero)
                        {
                            is64bit = Memory.IsProcess64Bit(procHnd); //we all know it's false, but still 
                        }
                    }
                }
            }
            else
            {
                var wndHnd = Memory.FindWindowClassName("Valve001");
                if (wndHnd != IntPtr.Zero)
                {
                    drawyourstuff = true;
                    wndMargins = Renderer.GetWindowMargins(wndHnd);
                    wndSize = Renderer.GetWindowSize(wndHnd);
                    isGameOnTop = Renderer.IsGameOnTop(wndHnd);

                    if (client_panorama == IntPtr.Zero)
                    {
                        client_panorama = Memory.GetModule(procHnd, "client_panorama.dll", is64bit);
                    }
                    else
                    {
                        if (client_panorama_size == IntPtr.Zero)
                        {
                            client_panorama_size = Memory.GetModuleSize(procHnd, "client_panorama.dll", is64bit);
                        }
                        else
                        {
                            if (dwViewMatrix_Offs == IntPtr.Zero)
                            {
                                dwViewMatrix_Offs = Memory.FindSignature(procHnd, client_panorama, client_panorama_size, "0F 10 05 ? ? ? ? 8D 85 ? ? ? ? B9", 0x3);
                            }
                            if (dwEntityList_Offs == IntPtr.Zero)
                            {
                                dwEntityList_Offs = Memory.FindSignature(procHnd, client_panorama, client_panorama_size, "BB ? ? ? ? 83 FF 01 0F 8C ? ? ? ? 3B F8", 0x1);
                            }
                        }    
                    }
                        
                }
                else
                {
                    Memory.CloseHandle(procHnd);
                    procHnd = IntPtr.Zero;
                    drawyourstuff = false;
                    //clear your offsets and modules too
                    client_panorama = IntPtr.Zero;
                    client_panorama_size = IntPtr.Zero;
                    dwViewMatrix_Offs = IntPtr.Zero;
                    dwEntityList_Offs = IntPtr.Zero;
                }
            }
        }



        private static void OnInput(VirtualKeyCode key, bool isPressed, EventArgs args)
        {
            if (key == VirtualKeyCode.End)
            {
                Renderer.ReleaseAllSprites();
            }
        }



        private static void OnRenderer(int fps, EventArgs args)
        {
            if (!drawyourstuff) return;
            if (!isGameOnTop) return;


            

            if (imageIndex > 0)
            {
                Renderer.DrawSprite(imageIndex, 20,20, new SharpDX.Color(255,255,0,125));
            }



            //Console.WriteLine(imageIndex);

            //TextureRendering.Render(new Vector2(127, 127), UltimateCircle);

            //Renderer.DrawText(imageBytesPLS[6].ToString("X"), 50, 50, 32, 0xFFFFFFFF);

            //Renderer.DrawText(fps.ToString(), wndMargins.X, wndMargins.Y, 16, 0xFF00FF00);

            //Renderer.DrawText(Dressed.ToString(), 620, 620, 16, 0xFFFFFFFF);

            var matrix = Memory.ReadMatrix(procHnd, (IntPtr)(dwViewMatrix_Offs.ToInt64() + 0xB0));
            //Vector3 headpos2 = new Vector3(0, 0, 0);
            //Vector2 vScreen_head2 = new Vector2(0, 0);
            //if (WorldToScreen(headpos2, out vScreen_head2, matrix, wndMargins, wndSize))
            //{
            //    Renderer.DrawRect(vScreen_head2.X - 1, vScreen_head2.Y - 1, 3, 3, 0xFFFFFFFF);
            //}
            if (dwEntityList_Offs != IntPtr.Zero)
            {
                for (uint i = 0; i <= 64; i++)
                {
                    var entityAddr = Memory.ReadPointer(procHnd, (IntPtr)(dwEntityList_Offs.ToInt64() + i * 0x10), is64bit);
                    if (entityAddr != IntPtr.Zero)
                    {
                        var health = Memory.ReadInt32(procHnd, (IntPtr)(entityAddr.ToInt64() + 0x100));
                        //Console.WriteLine($"{i.ToString()} - entityAddr: {entityAddr.ToString("X")}");
                        if ((health >= 1) && (health <= 100))
                        {
                            var feetpos = Memory.ReadVector3(procHnd, (IntPtr)(entityAddr.ToInt64() + 0x138));
                            Vector2 vScreen_head = new Vector2(0, 0);
                            Vector2 vScreen_foot = new Vector2(0, 0);
                            Vector3 headpos = new Vector3(feetpos.X, feetpos.Y, feetpos.Z + 70.0f);
                            if (WorldToScreen(headpos, out vScreen_head, matrix, wndMargins, wndSize))
                            {
                                if (WorldToScreen(feetpos, out vScreen_foot, matrix, wndMargins, wndSize))
                                {
                                    //var boxHeight = vScreen_foot.Y - vScreen_head.Y;
                                    //Renderer.DrawBox(vScreen_head.X - boxHeight / 4, vScreen_head.Y, boxHeight / 2, boxHeight, 4, 0x7FFF0000);
                                    Renderer.DrawRect(vScreen_head.X - 1, vScreen_head.Y - 1, 3, 3, new SharpDX.Color(0xFFFFFFFF));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
