//https://github.com/opentk/opentk
// NuGet\Install-Package OpenTK -Version 4.8.2
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

//https://github.com/StbSharp/StbImageSharp
// NuGet\Install-Package StbImageSharp -Version 2.30.15
using StbImageSharp;

namespace KeyEngine.Example
{
    public class Program
    {
        private static void Main(string[] args)
        {
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(640, 495),
                Title = "Example (Mouse wheel to zoom)",
            };

            using (Window mainWindow = new Window(nativeWindowSettings))
            {
                mainWindow.Run();
            }
        }
    }

    public class Window : GameWindow
    {
        private static float[] vertexData =
        {
            //  __________
            // |          |
            // |          |
            // |          |
            // |          |
            // |__________|
            //Pos           //Color         //TexCoords
             0.5f,  0.5f,   1, 1, 1, 1,     1, 1, // Up right
             0.5f, -0.5f,   1, 1, 1, 1,     1, 0, // Up left
            -0.5f, -0.5f,   1, 1, 1, 1,     0, 0, // Down left
            -0.5f,  0.5f,   1, 1, 1, 1,     0, 1, // Down right
        };

        private static sbyte[] indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        //Vao id
        private int vao;
        //Vbo id
        private int vbo;
        //Ebo id
        private int ebo;
        //Texture id
        private int texture;

        //!!!!! Path to texture !!!!!
        private static string path = "Assets/Player.png";

        //Ortho size (Zoom)
        private float orthoSize = 2;

        private string vertexSource =
        @"#version 330

            layout(location = 0) in vec2 a_Position;
            layout(location = 1) in vec4 a_Color;
            layout(location = 2) in vec2 a_TexCoords;
            
            out vec4 color;
            out vec2 texCoord;
            
            uniform mat4 u_Projection;
            
            void main() 
            {
              color = a_Color;
              gl_Position = u_Projection * vec4(a_Position, 0, 1.0f);
              texCoord = a_TexCoords;
            }";

        private string fragmentSource =
        @"#version 330

            in vec4 color;
            in vec2 texCoord;
            
            out vec4 outputColor;
            uniform sampler2D texture0;
            
            void main()
            {
              outputColor = texture(texture0, texCoord) * color;
            }";

        //Shader program id
        private int shaderProgram;

        public Window(NativeWindowSettings settings) : base(GameWindowSettings.Default, settings)
        {

        }

        protected override void OnLoad()
        {
            Console.WriteLine(ClientSize);
            base.OnLoad();

            //Gen
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            ebo = GL.GenBuffer();

            //Bind
            GL.BindVertexArray(vao);

            //Set vertex data
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            //Vertex pos
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            //Vertex color
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 8 * sizeof(float), 2 * sizeof(float));

            //Vertex tex coord
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            //Ebo
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(sbyte), indices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            //Texture
            texture = GL.GenTexture();

            //Bind
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            //Set nearest filter
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);

            //Set clampToEdge mode
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);

            StbImage.stbi_set_flip_vertically_on_load(1);

            using (Stream stream = File.OpenRead(path))
            {
                ImageResult imageResult = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, imageResult.Width, imageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, imageResult.Data);
            }

            //Shaders
            shaderProgram = GL.CreateProgram();

            //Vertex
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            //Fragment
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            GL.LinkProgram(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            float x = (float)ClientSize.X;
            float y = (float)ClientSize.Y;

            GL.ClearColor(0.3f, 0.2f, 0.7f, 1f);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            //Enable blend
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //Bind texture
            GL.BindTexture(TextureTarget.Texture2D, texture);

            //Bind shader
            GL.UseProgram(shaderProgram);

            float width = ClientSize.X; float height = ClientSize.Y;
            float aspectRatio = width / height;
            Matrix4 projection = Matrix4.CreateOrthographic(orthoSize * aspectRatio, orthoSize, -1, 1);
            Matrix4 model = Matrix4.CreateScale(new Vector3(1f, 1f, 1f));

            Matrix4 mp = model * projection;

            //Set projection
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "u_Projection"), false, ref mp);

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

            //Render
            GL.DrawElements(BeginMode.Triangles, indices.Length, DrawElementsType.UnsignedByte, 0);

            Context.SwapBuffers();

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.Disable(EnableCap.Blend);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.UseProgram(0);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            orthoSize -= MouseState.ScrollDelta.Y;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }
    }
}
