using EasyStorage;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace FileBuddyLib
{
	/// <summary>
	/// This class does all the heavy lifting for saving/loading files.
	/// </summary>
	public abstract class FileBuddy
	{
		#region Member Variables

		/// <summary>
		/// The save device.
		/// </summary>
		private IAsyncSaveDevice saveDevice;
		
		/// <summary>
		/// The location where this file will be saved/loaded from.
		/// </summary>
		public string Folder { get; set; }

		/// <summary>
		/// the filename to use
		/// </summary>
		public string File { get; set; }

		/// <summary>
		/// Flag for whether or not teh high scores have been loaded from a file
		/// </summary>
		/// <value><c>true</c> if loaded; otherwise, <c>false</c>.</value>
		private bool Loaded { get; set; }

		/// <summary>
		/// delegate method to write out to file, provided by user.
		/// </summary>
		/// <param name="myFileStream"></param>
		public FileAction SaveMethod { get; set; }

		/// <summary>
		/// delegate method to load from file, provided by user.
		/// </summary>
		/// <param name="myFileStream"></param>
		public FileAction LoadMethod { get; set; }

		#endregion //Member Variables

		#region Methods

		/// <summary>
		/// hello standard constructor!
		/// </summary>
		public FileBuddy(string fileLocation, string filename)
		{
			//set the save location
			Folder = fileLocation;
			File = fileLocation;

			Loaded = false;
		}

		/// <summary>
		/// called once at the beginning of the program
		/// gets the storage device
		/// </summary>
		/// <param name="myGame">the current game.</param>
		public virtual void Initialize(Game myGame)
		{
			// on Windows Phone we use a save device that uses IsolatedStorage
			// on Windows and Xbox 360, we use a save device that gets a shared StorageDevice to handle our file IO.
#if WINDOWS_PHONE || ANDROID
			saveDevice = new IsolatedStorageSaveDevice();
#else
			// create and add our SaveDevice
			SharedSaveDevice sharedSaveDevice = new SharedSaveDevice();
			myGame.Components.Add(sharedSaveDevice);

			// make sure we hold on to the device
			saveDevice = sharedSaveDevice;

			// hook two event handlers to force the user to choose a new device if they cancel the
			// device selector or if they disconnect the storage device after selecting it
			sharedSaveDevice.DeviceSelectorCanceled += (s, e) => e.Response = SaveDeviceEventResponse.Force;
			sharedSaveDevice.DeviceDisconnected += (s, e) => e.Response = SaveDeviceEventResponse.Force;

			// prompt for a device on the first Update we can
			sharedSaveDevice.PromptForDevice();
#endif

#if XBOX
			// add the GamerServicesComponent
			Components.Add(new Microsoft.Xna.Framework.GamerServices.GamerServicesComponent(this));
#endif

			// hook an event so we can see that it does fire
			saveDevice.SaveCompleted += new SaveCompletedEventHandler(saveDevice_SaveCompleted);
		}
		
		/// <summary>
		/// event handler that gets fired off when a write op is completed
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Arguments.</param>
		void saveDevice_SaveCompleted(object sender, FileActionCompletedEventArgs args)
		{
			//Write a message out to the deubg log so we know whats going on
			string strText = "SaveCompleted!";
			if (null != args.Error)
			{
				strText = args.Error.Message;
			}

			Debug.WriteLine(strText);
		}

		#endregion //Methods

		#region XML Methods

		/// <summary>
		/// Save all the high scores out to disk
		/// </summary>
		public void Save()
		{
			Debug.Assert(null != SaveMethod);

			// make sure the device is ready
			if (saveDevice.IsReady)
			{
				//save a file asynchronously. 
				//this will trigger IsBusy to return true for the duration of the save process.
				saveDevice.SaveAsync(
					Folder,
					File,
					SaveMethod);
			}
		}

		/// <summary>
		/// load all the data from disk
		/// </summary>
		public void Load()
		{
			Debug.Assert(null != LoadMethod);

			if (!Loaded)
			{
				try
				{
					//if there is a file there, load it into the system
					if (saveDevice.FileExists(Folder, File))
					{
						saveDevice.Load(
							Folder,
							File,
							LoadMethod);
					}

					//set the Loaded flag to true since high scores only need to be laoded once
					Loaded = true;
					Debug.WriteLine("Loaded file" + Folder + "\"" + File);
				}
				catch (Exception ex)
				{
					//now you fucked up
					Loaded = false;

					// just write some debug output for our verification
					Debug.WriteLine(ex.Message);
				}
			}
		}

		#endregion //XML Methods
	}
}