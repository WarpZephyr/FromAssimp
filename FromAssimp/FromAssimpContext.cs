using Assimp;
using FromAssimp.Helpers;
using SoulsFormats;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using AssimpMatrix4x4 = Assimp.Matrix4x4;

namespace FromAssimp
{
    /// <summary>
    /// An <see cref="AssimpContext"/> wrapper for adding FromSoftware model support.
    /// </summary>
    public class FromAssimpContext : IDisposable
    {
        /// <summary>
        /// The underlying <see cref="AssimpContext"/>.
        /// </summary>
        public AssimpContext Context { get; set; }

        /// <summary>
        /// Whether or not to do the check flip fix during FLVER0 face triangulation.
        /// </summary>
        public bool DoCheckFlip { get; set; }

        /// <summary>
        /// Whether or not the underlying <see cref="AssimpContext"/> has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Create a new <see cref="FromAssimpContext"/>.
        /// </summary>
        public FromAssimpContext()
        {
            Context = new AssimpContext();
        }

        /// <summary>
        /// Create a new <see cref="FromAssimpContext"/> from the given <see cref="AssimpContext"/>.
        /// </summary>
        public FromAssimpContext(AssimpContext context)
        {
            if (context.IsDisposed)
            {
                throw new InvalidOperationException($"{nameof(AssimpContext)} {nameof(context)} is already disposed.");
            }

            Context = context;
        }

        /// <summary>
        /// Import a model from the specified path.
        /// </summary>
        /// <param name="path">The path to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFile(string path, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            if (MDL4.IsRead(path, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(path, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(path, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(path, out FLVER2 flver2))
                return flver2.ToAssimpScene();
            
            return Context.ImportFile(path, postProcessFlags);
        }

        /// <summary>
        /// Import a model from a <see cref="Stream"/>. Converts the <see cref="Stream"/> into a byte array for FromSoftware model reading.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromStream(Stream stream, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            if (MDL4.IsRead(stream, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(stream, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(stream, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(stream, out FLVER2 flver2))
                return flver2.ToAssimpScene();
            
            return Context.ImportFileFromStream(stream, postProcessFlags);
        }

        /// <summary>
        /// Import a model from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array to import a model from.</param>
        /// <param name="postProcessFlags">The post processing steps to take while importing, only for non-FromSoftware models for now.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromBytes(byte[] bytes, PostProcessSteps postProcessFlags = PostProcessSteps.None)
        {
            if (MDL4.IsRead(bytes, out MDL4 mdl4))
                return mdl4.ToAssimpScene();
            else if (SMD4.IsRead(bytes, out SMD4 smd4))
                return smd4.ToAssimpScene();
            else if (FLVER0.IsRead(bytes, out FLVER0 flver0))
                return flver0.ToAssimpScene();
            else if (FLVER2.IsRead(bytes, out FLVER2 flver2))
                return flver2.ToAssimpScene();

            using var ms = new MemoryStream(bytes);
            return Context.ImportFileFromStream(ms, postProcessFlags);
        }

        /// <summary>
        /// Import a file from a <see cref="IFlver"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        /// <exception cref="NotSupportedException">The type using the <see cref="IFlver"/> interface was not supported.</exception>
        public static Scene ImportFileFromIFlver(IFlver model)
        {
            if (model is FLVER0 flver0)
                return flver0.ToAssimpScene();
            else if (model is FLVER2 flver2)
                return flver2.ToAssimpScene();

            throw new NotSupportedException($"The type {model.GetType().Name} using the {nameof(IFlver)} interface is not supported for {nameof(ImportFileFromIFlver)}");
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER0"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public Scene ImportFileFromFlver0(FLVER0 model)
        {
            var scene = FlverTest.TestFlver0(model, DoCheckFlip, true, FlverTest.MirrorX);
            //SceneHelper.DebugPrintSceneInfo(scene);
            return scene;
        }

        /// <summary>
        /// Import a file from a <see cref="FLVER2"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public static Scene ImportFileFromFlver2(FLVER2 model)
        {
            var scene = model.ToAssimpScene();
            //SceneHelper.DebugPrintSceneInfo(scene);
            return scene;
        }

        /// <summary>
        /// Import a file from a <see cref="MDL4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public static Scene ImportFileFromMdl4(MDL4 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Import a file from a <see cref="SMD4"/> model.
        /// </summary>
        /// <param name="model">The model to import from.</param>
        /// <returns>A <see cref="Scene"/>.</returns>
        public static Scene ImportFileFromSmd4(SMD4 model)
        {
            return model.ToAssimpScene();
        }

        /// <summary>
        /// Exports a scene to the specified format and writes it to a file. Currently does not support exporting From models.
        /// </summary>
        /// <param name="scene">The scene to export.</param>
        /// <param name="path">The path to write the exported file to.</param>
        /// <param name="exportFormatId">The format to write the exported file in.</param>
        /// <returns>Whether or not the export was successful.</returns>
        public bool ExportFile(Scene scene, string path, string exportFormatId)
        {
            return Context.ExportFile(scene, path, exportFormatId);
        }

        /// <summary>
        /// Get the appropiate extension for the chosen format.
        /// </summary>
        /// <param name="format">The format to get the extension of.</param>
        /// <returns>The extension of the given format.</returns>
        public static string GetFormatExtension(string format)
        {
            return format switch
            {
                "fbx" => "fbx",
                "fbxa" => "fbx",
                "collada" => "dae",
                "obj" => "obj",
                _ => format
            };
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the underlying <see cref="AssimpContext"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    Context.Dispose();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="AssimpContext"/>.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}