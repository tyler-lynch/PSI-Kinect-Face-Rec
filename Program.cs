// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace KinectFaceRec
{
    using System;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;

    /// <summary>
    /// Kinect sample program.
    /// </summary>
    public class Program
    {
        // Variables related to Psi
        private const string ApplicationName = "PSIKinectFaceRec";

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            Program prog = new Program();
            prog.PerformFaceDetection();
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineCompleted"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline completion event arguments.</param>
        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineExceptionNotHandled"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline exception event arguments.</param>
        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        /// <summary>
        /// This is the main code for the Face Detection.
        /// </summary>
        private void PerformFaceDetection()
        {
            Console.WriteLine("Initializing Psi.");

            //bool detected = false;

            // First create our \Psi pipeline
            using (var pipeline = Pipeline.Create("FaceDetection"))
            {
                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Next create our Kinect sensor. We will be using the color images, face tracking, and audio from the Kinect sensor
                var kinectSensorConfig = new KinectSensorConfiguration
                {
                    OutputColor = true,
                    OutputAudio = true,
                    OutputBodies = true, 
                };

                var kinectSensor = new KinectSensor(pipeline, kinectSensorConfig);
                var kinectFaceDetector = new Microsoft.Psi.Kinect.Face.KinectFaceDetector(pipeline, kinectSensor, Microsoft.Psi.Kinect.Face.KinectFaceDetectorConfiguration.Default);

                var faceRectangles = kinectFaceDetector.Faces.Select(faceList => faceList.Select(face => new System.Drawing.Rectangle(
                    face.FaceBoundingBoxInColorSpace.Left,
                    face.FaceBoundingBoxInColorSpace.Top,
                    face.FaceBoundingBoxInColorSpace.Right - face.FaceBoundingBoxInColorSpace.Left,
                    face.FaceBoundingBoxInColorSpace.Bottom - face.FaceBoundingBoxInColorSpace.Top
                )).ToList());

                


                // ********************************************************************
                // Finally create a Live Visualizer using PsiStudio.
                // We must persist our streams to a store in order for Live Viz to work properly
                // ********************************************************************

                // Create store for the data. Live Visualizer can only read data from a store.
                var pathToStore = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var store = PsiStore.Create(pipeline, ApplicationName, pathToStore);

                faceRectangles.Write("BoundingBox", store);

                kinectSensor.Audio.Write("Audio", store);

                var images = kinectSensor.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                images.Write("Images", store, true, DeliveryPolicy.LatestMessage);


                // Run the pipeline
                pipeline.RunAsync();

                Console.WriteLine($"Store will be saved to: {store.Path}");
                Console.WriteLine("Press any key to finish recording");
                Console.ReadKey();
            }
        }
    }
}
