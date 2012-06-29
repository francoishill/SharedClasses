using System.IO;
using System.Data.SQLite;
using System;
using System.Data;

namespace SharedClasses
{
	public class SqliteDatabase
	{
		private string sqliteFilePath;
		private string password;

		private SQLiteConnection connection;
		private SQLiteCommand command;
		private SQLiteDataAdapter dataAdap;
		private DataSet dataSet = new DataSet();
		private DataTable dataTable = new DataTable();

		public SqliteDatabase(string sqliteFilePath, string password)
		{
			this.sqliteFilePath = sqliteFilePath;
			this.password = password;
		}

		private void SetConnection()
		{
			if (!File.Exists(this.sqliteFilePath))
				SQLiteConnection.CreateFile(this.sqliteFilePath);

			connection = new SQLiteConnection(
				@"Data Source=" + this.sqliteFilePath
				+ ";Version=3;New=False;Compress=True;");
		}

		public bool ExecuteQuery(string txtQuery)
		{
			SetConnection();
			if (OpenConnection())
			{
				try
				{
					this.connection.ChangePassword(password);
					this.command = this.connection.CreateCommand();
					this.command.CommandText = txtQuery;
					this.command.ExecuteNonQuery();
					this.connection.Close();
					return true;
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Error performing SQL query: " + exc.Message);
					return false;
				}
			}
			return false;
		}

		private bool OpenConnection()
		{
			try
			{
				this.connection.SetPassword(password);
				this.connection.Open();
				return true;
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Error opening database: " + exc.Message);
				return false;
			}
		}

		public DataView GetDataView(string tablename)
		{
			try
			{
				SetConnection();
				if (OpenConnection())
				{
					command = connection.CreateCommand();
					string CommandText = "select * FROM [" + tablename + "]";
					dataAdap = new SQLiteDataAdapter(CommandText, connection);

					SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(dataAdap);
					dataAdap.InsertCommand = commandBuilder.GetInsertCommand();
					try
					{
						dataAdap.DeleteCommand = commandBuilder.GetDeleteCommand();
					}
					catch (Exception exc) { UserMessages.ShowErrorMessage("Error creating delete command for table: " + exc.Message); }
					try
					{
						dataAdap.UpdateCommand = commandBuilder.GetUpdateCommand();
					}
					catch (Exception exc) { UserMessages.ShowErrorMessage("Error creating update command for table: " + exc.Message); }

					dataSet.Reset();
					dataAdap.Fill(dataSet);
					dataTable = dataSet.Tables[0];
					//Grid.DataSource = DT;
					//dataGrid1.DataContext = DT;
					return dataTable.DefaultView;
				}
				else
					return null;
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage("Error getting data from table: " + exc.Message);
				return null;
			}
			finally
			{
				connection.Close();
			}
		}
	}
}