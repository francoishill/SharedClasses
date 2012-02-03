using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for ExploreFaceTrainingDatabaseWindow.xaml
	/// </summary>
	public partial class ExploreFaceTrainingDatabaseWindow : Window
	{
		private SQLiteConnection sql_con;
		private SQLiteCommand sql_cmd;
		private SQLiteDataAdapter DB;
		private DataSet DS = new DataSet();
		private DataTable DT = new DataTable();

		public ExploreFaceTrainingDatabaseWindow()
		{
			InitializeComponent();

			//System.Data.SQLite.SQLiteDataAdapter
		}

		//private DataSet _ds;

		//protected override void OnInitialized(EventArgs e)
		//{
		//	base.OnInitialized(e);

		//	_ds = new DataSet();
		//	DataTable dt = new DataTable();
		//	_ds.Tables.Add(dt);

		//	DataColumn cl = new DataColumn("Col1", typeof(string));
		//	cl.MaxLength = 100;
		//	dt.Columns.Add(cl);

		//	cl = new DataColumn("Col2", typeof(string));
		//	cl.MaxLength = 100;
		//	dt.Columns.Add(cl);

		//	DataRow rw = dt.NewRow();
		//	dt.Rows.Add(rw);
		//	rw["Col1"] = "Value1";
		//	rw["Col2"] = "Value2";


		//	dataGrid1.ItemsSource = _ds.Tables[0].DefaultView;
		//}  

		//private string FullPathToSqliteDatabase = @"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestSqliteCS\TestSqliteCS\bin\Debug\Northwind.sl3";
		//private string FullPathToSqliteDatabase = @"C:\Users\francois\Documents\Visual Studio 2010\Projects\TestSqliteCS\TestSqliteCS\bin\Debug\tshwane201110current.sqlite";
		private string FullPathToSqliteDatabase = @"C:\Francois\other\Tmp\tmp.sqlite";
		private void SetConnection()
		{
			//MessageBox.Show(@"Data Source=""D:\Francois\Dev\VSprojects\TestSqliteCS\TestSqliteCS\Northwind.sl3"";Version=3;New=False;Compress=True;");

			if (!File.Exists(FullPathToSqliteDatabase))// || MessageBox.Show("The file already exists, overwrite it?" + Environment.NewLine, "Confirm overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				SQLiteConnection.CreateFile(@"C:\Francois\other\Tmp\tmp.sqlite");

			sql_con = new SQLiteConnection
				//(@"Data Source=Northwind.sl3;Version=3;New=False;Compress=True;");
				(@"Data Source=" + FullPathToSqliteDatabase + ";Version=3;New=False;Compress=True;");
		}

		private void ExecuteQuery(string txtQuery)
		{
			SetConnection();
			if (OpenConnection())
			{
				sql_con.ChangePassword("mypassword");
				sql_cmd = sql_con.CreateCommand();
				sql_cmd.CommandText = txtQuery;
				sql_cmd.ExecuteNonQuery();
				sql_con.Close();
			}
		}

		private ObservableCollection<string> GetListOfTablenames()
		{
			ObservableCollection<string> tmpList = new ObservableCollection<string>();

			SetConnection();
			if (OpenConnection())
			{
				sql_cmd = sql_con.CreateCommand();
				DB = new SQLiteDataAdapter("SELECT name FROM SQLITE_MASTER where type='table'", sql_con);

				DS.Reset();
				DB.Fill(DS);
				DT = DS.Tables[0];
				sql_con.Close();

				foreach (DataRow row in DT.Rows)
					tmpList.Add(row[0].ToString());
			}
			return tmpList;
		}

		private bool OpenConnection()
		{
			try
			{
				sql_con.SetPassword("mypassword");
				sql_con.Open();
				return true;
			}
			catch (Exception exc)
			{
				MessageBox.Show("Error opening database: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		private void LoadData(string tablename)
		{
			try
			{
				dataGrid1.ItemsSource = null;

				SetConnection();
				if (OpenConnection())
				{
					sql_cmd = sql_con.CreateCommand();
					string CommandText = "select * FROM [" + tablename + "]";
					DB = new SQLiteDataAdapter(CommandText, sql_con);

					SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(DB);
					DB.InsertCommand = commandBuilder.GetInsertCommand();
					try
					{
						DB.DeleteCommand = commandBuilder.GetDeleteCommand();
					}
					catch (Exception exc) { MessageBox.Show("Error creating delete command for table: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
					try
					{
						DB.UpdateCommand = commandBuilder.GetUpdateCommand();
					}
					catch (Exception exc) { MessageBox.Show("Error creating update command for table: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }

					DS.Reset();
					DB.Fill(DS);
					DT = DS.Tables[0];
					//Grid.DataSource = DT;
					//dataGrid1.DataContext = DT;
					dataGrid1.ItemsSource = DT.DefaultView;
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show("Error getting data from table: " + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				sql_con.Close();
			}
		}

		private void Add()
		{
			//string txtSQLQuery = "insert into  mains (desc) values ('" + txtDesc.Text + "')";
			//ExecuteQuery(txtSQLQuery);
		}

		private void CreateRequiredTables()
		{
			ExecuteQuery("CREATE TABLE facedetection (ind INTEGER PRIMARY KEY ASC, name TEXT, image BLOB)");
			ExecuteQuery("CREATE TABLE metadata (key TEXT PRIMARY KEY ASC, value TEXT)");
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//MessageBox.Show(@"Data Source=""D:\Francois\Dev\VSprojects\TestSqliteCS\TestSqliteCS\Northwind.sl3"";Version=3;New=False;Compress=True;");
			//SetConnection();

			if (GetListOfTablenames().Count == 0)
			{
				CreateRequiredTables();
			}
			else
			{
				listBox1.ItemsSource = null;
				listBox1.ItemsSource = GetListOfTablenames();
			}

			/*string Passphrase = "abcdefghijklmnopqrstuvwxyz1234567890";
			string Salt = "abcdefghijklmnopqrstuvwxyz1234567890";

			string filepathToConcatenatedImagesFile = @"C:\Francois\Other\tmp\tmpfile";
			if (!File.Exists(filepathToConcatenatedImagesFile) || UserMessages.Confirm("The following file exists, do you want to overwrite it?" + Environment.NewLine + filepathToConcatenatedImagesFile))
			{
				using (FileStream fs = new FileStream(filepathToConcatenatedImagesFile, FileMode.Create))
				{
					using (BinaryWriter bw = new BinaryWriter(fs))
					{
						//int cnt = 1;
						foreach (string pic in Directory.GetFiles(@"C:\Francois\Other\tmp\Pics", "*.bmp"))//C:\Users\francois\Pictures\Temp iPhone 4", "*.jpg"))
						{
							//FileInfo fi = new FileInfo(pic);
							//Int64 fileSize = fi.Length;
							byte[] fileBytes = EncryptBytes(File.ReadAllBytes(pic), Passphrase, Salt);

							//EncryptBytes(ref fileBytes);
							//RSAEncrypt(fileBytes, RSA.ExportParameters(false), false);//Passphrase, Salt);

							bw.Write(fileBytes.Length);
							bw.Write(fileBytes);
							//bw.Write(fileSize);
							//bw.Write(File.ReadAllBytes(pic));
							//if (cnt++ == 5)
							//	break;
						}
					}
				}
			}

			using (FileStream fs = new FileStream(@"C:\Francois\Other\tmp\tmpfile", FileMode.Open))
			{
				using (BinaryReader br = new BinaryReader(fs))
				{
					//int cnt = 1;
					//while (br.PeekChar() != -1)
					//{
					//Int64 imagebyteLength = br.ReadInt64();
					Int32 imagebyteLength = 0;

					bool EOF = false;

					int cnt = 1;
					foreach (string fileToDelete in Directory.GetFiles(@"C:\Francois\Other\tmp", "tmppic*.jpg"))
						File.Delete(fileToDelete);
					while (!EOF)
					{
						try
						{
							imagebyteLength = br.ReadInt32();
							byte[] fileBytes = DecryptBytes(br.ReadBytes(imagebyteLength), Passphrase, Salt);
							//DecryptBytes(ref fileBytes);					
							File.WriteAllBytes(
								@"C:\Francois\Other\tmp\tmppic" + cnt++ + ".jpg",
								fileBytes);//RSADecrypt(fileBytes, RSA.ExportParameters(true), false));//Passphrase, Salt));
						}
						catch (EndOfStreamException)
						{
							EOF = true;
						}
						catch (Exception exc)
						{
							MessageBox.Show("Error reading bytes: " + exc.Message);
						}
					}
					//}
					//FileInfo fi = new FileInfo(pic);
					//Int64 fileSize = fi.Length;
					//byte[] EncryptedBytes = EncryptBytes(File.ReadAllBytes(pic), "SensitivePhrase", "SodiumChloride");
					//bw.Write(EncryptedBytes.Length);
					//bw.Write(EncryptedBytes);
					//break;
				}
			}*/

			//LoadData();
		}

		/*public static void EncryptBytes(ref byte[] data)
		{
			for (int i = 0; i < data.Length; i++)
				data[i] = (byte)(data[i] ^ (byte)0xA9);
		}

		public static void DecryptBytes(ref byte[] data)
		{
			for (int i = 0; i < data.Length; i++)
				data[i] = (byte)(data[i] ^ (byte)0xA9);
		}*/

		/*static public byte[] RSAEncrypt(byte[] DataToEncrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
		{
			try
			{
				byte[] encryptedData;
				//Create a new instance of RSACryptoServiceProvider.
				using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
				{

					//Import the RSA Key information. This only needs
					//toinclude the public key information.
					RSA.ImportParameters(RSAKeyInfo);

					//Encrypt the passed byte array and specify OAEP padding.  
					//OAEP padding is only available on Microsoft Windows XP or
					//later.  
					encryptedData = RSA.Encrypt(DataToEncrypt, DoOAEPPadding);
				}
				return encryptedData;
			}
			//Catch and display a CryptographicException  
			//to the console.
			catch (CryptographicException e)
			{
				Console.WriteLine(e.Message);
				MessageBox.Show("Error encrypting: " + e.Message);

				return null;
			}

		}

		static public byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
		{
			try
			{
				byte[] decryptedData;
				//Create a new instance of RSACryptoServiceProvider.
				using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
				{
					//Import the RSA Key information. This needs
					//to include the private key information.
					RSA.ImportParameters(RSAKeyInfo);

					//Decrypt the passed byte array and specify OAEP padding.  
					//OAEP padding is only available on Microsoft Windows XP or
					//later.  
					decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
				}
				return decryptedData;
			}
			//Catch and display a CryptographicException  
			//to the console.
			catch (CryptographicException e)
			{
				Console.WriteLine(e.ToString());
				MessageBox.Show("Error encrypting: " + e.Message);

				return null;
			}

		}*/

		// Example usage: EncryptBytes(someFileBytes, "SensitivePhrase", "SodiumChloride");
		public static byte[] EncryptBytes(byte[] inputBytes, string passPhrase, string saltValue)
		{
			RijndaelManaged RijndaelCipher = new RijndaelManaged();

			RijndaelCipher.Mode = CipherMode.CBC;
			byte[] salt = Encoding.ASCII.GetBytes(saltValue);
			PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, salt, "SHA1", 2);

			ICryptoTransform Encryptor = RijndaelCipher.CreateEncryptor(password.GetBytes(32), password.GetBytes(16));

			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, Encryptor, CryptoStreamMode.Write);
			cryptoStream.Write(inputBytes, 0, inputBytes.Length);
			cryptoStream.FlushFinalBlock();
			byte[] CipherBytes = memoryStream.ToArray();

			memoryStream.Close();
			cryptoStream.Close();

			return CipherBytes;
		}

		// Example usage: DecryptBytes(encryptedBytes, "SensitivePhrase", "SodiumChloride");
		public static byte[] DecryptBytes(byte[] inputBytes, string passPhrase, string saltValue)
		{
			RijndaelManaged RijndaelCipher = new RijndaelManaged();

			RijndaelCipher.Mode = CipherMode.CBC;
			byte[] salt = Encoding.ASCII.GetBytes(saltValue);
			PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, salt, "SHA1", 2);

			ICryptoTransform Decryptor = RijndaelCipher.CreateDecryptor(password.GetBytes(32), password.GetBytes(16));

			MemoryStream memoryStream = new MemoryStream(inputBytes);
			CryptoStream cryptoStream = new CryptoStream(memoryStream, Decryptor, CryptoStreamMode.Read);
			byte[] plainBytes = new byte[inputBytes.Length];

			int DecryptedCount = cryptoStream.Read(plainBytes, 0, plainBytes.Length);

			memoryStream.Close();
			cryptoStream.Close();

			return plainBytes;
		}

		private void dataGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 1)
			{
				DataRow dr = (DataRow)((System.Data.DataRowView)((DataGrid)sender).SelectedItem).Row;
				object obj = dr[2];
				//MessageBox.Show(obj.GetType().Name);
				if (!(obj is Byte[]))
					return;

				byte[] imageBytes = obj as Byte[];
				MemoryStream ms = new MemoryStream(imageBytes);
				Bitmap bmap = new Bitmap(ms);
				//pictureBoxImage.Image = (System.Drawing.Image)bmap;
				image1.Source = ToBitmapSource(bmap);

				//do your stuff here using the dr variable
			}
		}

		private void dataGrid1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//if (((DataGrid)sender).SelectedItem == null)
			//	return;

			//DataGrid dg = (DataGrid)sender;
			//if (dg == null)
			//	return;
			//DataRowView drv1 = (DataRowView)dg.SelectedItem;
			//DataRow dr1 = drv1.Row;
			//if (dr1.IsNull(1)
			//DataRow dr1 = (DataRow)((System.Data.DataRowView)((DataGrid)sender).SelectedItem).Row;
			//if (dr1.IsNull(1))
			//{

			DataTable table = ((sender as DataGrid).ItemsSource as DataView).Table;
			DataRow dr1 = table.NewRow();
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "Image files|*.jpg;*.bmp;*.png";
			ofd.Title = "Please select an image for current record";
			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			if (ofd.ShowDialog(this) == true)
			{
				string FileLocation = ofd.FileName;//@"C:\Users\francois\Pictures\Temp iPhone 1\IMG_0009.JPG";
				using (FileStream stream = File.Open(FileLocation, FileMode.Open))
				{
					FileInfo info = new FileInfo(FileLocation);
					long fileSize = info.Length;
					//reasign the filesize to calculated filesize
					int maxImageSize = (Int32)fileSize;

					BinaryReader br = new BinaryReader(stream);
					byte[] data = br.ReadBytes(maxImageSize);
					//index, name, image
					dr1[1] = System.IO.Path.GetFileName(ofd.FileName);// "My name";
					dr1[2] = data;

					table.Rows.Add(dr1);

					//Save data
					//sql_con.Open();
					if (OpenConnection())
					{
						DB.Update(DS);
						sql_con.Close();
					}
				}
			}
			//}
		}

		/// <summary>
		/// Converts a <see cref="System.Drawing.Image"/> into a WPF <see cref="BitmapSource"/>.
		/// </summary>
		/// <param name="source">The source image.</param>
		/// <returns>A BitmapSource</returns>
		public static BitmapSource ToBitmapSource(System.Drawing.Image source)
		{
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(source);

			var bitSrc = ToBitmapSource(bitmap);

			bitmap.Dispose();
			bitmap = null;

			return bitSrc;
		}

		/// <summary>
		/// Converts a <see cref="System.Drawing.Bitmap"/> into a WPF <see cref="BitmapSource"/>.
		/// </summary>
		/// <remarks>Uses GDI to do the conversion. Hence the call to the marshalled DeleteObject.
		/// </remarks>
		/// <param name="source">The source bitmap.</param>
		/// <returns>A BitmapSource</returns>
		public static BitmapSource ToBitmapSource(System.Drawing.Bitmap source)
		{
			BitmapSource bitSrc = null;

			var hBitmap = source.GetHbitmap();

			try
			{
				bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap,
					IntPtr.Zero,
					Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			}
			catch (Win32Exception)
			{
				bitSrc = null;
			}
			finally
			{
				NativeMethods.DeleteObject(hBitmap);
			}

			return bitSrc;
		}

		/// <summary>
		/// FxCop requires all Marshalled functions to be in a class called NativeMethods.
		/// </summary>
		internal static class NativeMethods
		{
			[DllImport("gdi32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool DeleteObject(IntPtr hObject);
		}

		private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;
			LoadData(e.AddedItems[0].ToString());
		}

		private void buttonTrainFaces_Click(object sender, RoutedEventArgs e)
		{
			FaceTrainingForm.ShowFacetraining();
		}
	}
}
