﻿// Copyright (c) 2018, 2021, Oracle and/or its affiliates.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License, version 2.0, as
// published by the Free Software Foundation.
//
// This program is also distributed with certain software (including
// but not limited to OpenSSL) that is licensed under separate terms,
// as designated in a particular file or component or in included license
// documentation.  The authors of MySQL hereby grant you an
// additional permission to link the program and your derivative works
// with the separately licensed software that they have included with
// MySQL.
//
// Without limiting anything contained in the foregoing, this file,
// which is part of MySQL Connector/NET, is also subject to the
// Universal FOSS Exception, version 1.0, a copy of which can be found at
// http://oss.oracle.com/licenses/universal-foss-exception.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License, version 2.0, for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin St, Fifth Floor, Boston, MA 02110-1301  USA

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using MySql.Data.Common;
using MySqlX.XDevAPI;
using NUnit.Framework;

namespace MySql.Data.MySqlClient.Tests
{
  public class SslTests : TestBase
  {
    private string _sslCa;
    private string _sslCert;
    private string _sslKey;

    public SslTests()
    {
      string cPath = string.Empty;
      cPath = Assembly.GetExecutingAssembly().Location.Replace(String.Format("{0}.dll",
        Assembly.GetExecutingAssembly().GetName().Name), string.Empty);

      _sslCa = cPath + "ca.pem";
      _sslCert = cPath + "client-cert.pem";
      _sslKey = cPath + "client-key.pem";
    }

    #region General

    [Test]
    [Property("Category", "Security")]
    public void SslModePreferredByDefault()
    {
      MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", Connection);
      using (MySqlDataReader reader = command.ExecuteReader())
      {
        Assert.True(reader.Read());
        StringAssert.StartsWith("TLSv1", reader.GetString(1));
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslModeOverriden()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.AllowPublicKeyRetrieval = true;
      builder.Database = "";
      using (MySqlConnection connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", connection);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          Assert.AreEqual(string.Empty, reader.GetString(1));
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void RepeatedSslConnectionOptionsNotAllowed()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslca={_sslCa}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificateFile = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatefile={_sslCa}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "certificatefile"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatefile={_sslCa}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "certificatefile"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificateFile = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslca={_sslCa}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslcert={_sslCert};sslkey={_sslKey};sslca={_sslCa}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "sslca"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.CertificatePassword = "pass";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};certificatepassword=pass"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "certificatepassword"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCert = _sslCert;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslcert={_sslCert}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "sslcert"), exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslKey = _sslKey;
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection($"{builder.ConnectionString};sslkey={_sslKey}"));
      Assert.AreEqual(string.Format(Resources.DuplicatedSslConnectionOption, "sslkey"), exception.Message);
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidOptionsWhenSslDisabled()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslCa = "value";
      var exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.AreEqual(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslCert = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.AreEqual(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.SslKey = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.AreEqual(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.CertificateFile = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.AreEqual(Resources.InvalidOptionWhenSslDisabled, exception.Message);

      builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.None;
      builder.CertificatePassword = "value";
      exception = Assert.Throws<ArgumentException>(() => new MySqlConnection(builder.ConnectionString));
      Assert.AreEqual(Resources.InvalidOptionWhenSslDisabled, exception.Message);
    }

    #endregion

    #region PFX

    /// <summary>
    /// A client can connect to MySQL server using SSL and a pfx file.
    /// <remarks>
    /// This test requires starting the server with SSL support.
    /// For instance, the following command line enables SSL in the server:
    /// mysqld --no-defaults --standalone --console --ssl-ca='MySQLServerDir'\mysql-test\std_data\cacert.pem --ssl-cert='MySQLServerDir'\mysql-test\std_data\server-cert.pem --ssl-key='MySQLServerDir'\mysql-test\std_data\server-key.pem
    /// </remarks>
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void CanConnectUsingFileBasedCertificate()
    {
      string connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

        command = new MySqlCommand("show variables like 'have_ssl';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.AreEqualIgnoringCase("YES", reader.GetString(1));
        }

        command = new MySqlCommand("show variables like 'tls_version'", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.Contains("TLS", reader.GetString(1));
        }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingPFXCertificates()
    {
      string connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=pass;SSL Mode=Required;";
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
        MySqlCommand command = new MySqlCommand("SHOW SESSION STATUS LIKE 'Ssl_version';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.StartsWith("TLSv1", reader.GetString(1));
        }

        command = new MySqlCommand("show variables like 'have_ssl';", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.AreEqualIgnoringCase("YES", reader.GetString(1));
        }

        command = new MySqlCommand("show variables like 'tls_version'", c);
        using (MySqlDataReader reader = command.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.Contains("TLS", reader.GetString(1));
        }
      }

      connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client-incorrect.pfx;CertificatePassword=pass;SSL Mode=" + MySqlSslMode.VerifyCA;
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        Assert.Catch(() => c.Open());
      }

      connstr = Settings.ConnectionString;
      connstr += ";CertificateFile=client.pfx;CertificatePassword=WRONGPASSWORD;SSL Mode=" + MySqlSslMode.VerifyCA;
      using (MySqlConnection c = new MySqlConnection(connstr))
      {
        Assert.Catch(() => c.Open());
      }
    }

    /// <summary>
    /// Bug#31954655 - NET CONNECTOR - THUMBPRINT OPTION DOES NOT CHECK THUMBPRINT
    /// </summary>
    [Test]
    [Property("Category", "Security")]
    public void InvalidCertificateThumbprint()
    {
#if NETCOREAPP3_1 || NET5_0
      if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) Assert.Ignore();
#endif

      // Create a mock of certificate store
      string assemblyPath = TestContext.CurrentContext.TestDirectory;
      var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
      store.Open(OpenFlags.ReadWrite);
      var certificate = new X509Certificate2(assemblyPath + "\\client.pfx", "pass");
      store.Add(certificate);

      MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      csb.CertificateStoreLocation = MySqlCertificateStoreLocation.CurrentUser;
      csb.CertificateThumbprint = "spaghetti";

      // Throws an exception with and invalid/incorrect Thumbprint
      var ex = Assert.Throws<MySqlException>(() => new MySqlConnection(csb.ConnectionString).Open());
      Assert.That(ex.Message, Is.EqualTo(string.Format(Resources.InvalidCertificateThumbprint, csb.CertificateThumbprint)));

      csb.CertificateThumbprint = certificate.Thumbprint;
      using (var conn = new MySqlConnection(csb.ConnectionString))
      {
        conn.Open();
        Assert.AreEqual(ConnectionState.Open, conn.connectionState);
      }
    }

    #endregion

    #region PEM

    [Test]
    [Property("Category", "Security")]
    public void SslCertificateConnectionOptionsExistAndDefaultToNull()
    {
      var builder = new MySqlConnectionStringBuilder();

      // Options exist.
      Assert.True(builder.values.ContainsKey("sslca"));
      Assert.True(builder.values.ContainsKey("sslcert"));
      Assert.True(builder.values.ContainsKey("sslkey"));

      // Options default to null.
      Assert.Null(builder["sslca"]);
      Assert.Null(builder["sslcert"]);
      Assert.Null(builder["sslkey"]);
      Assert.Null(builder["ssl-ca"]);
      Assert.Null(builder["ssl-cert"]);
      Assert.Null(builder["ssl-key"]);
      Assert.Null(builder.SslCa);
      Assert.Null(builder.SslCert);
      Assert.Null(builder.SslKey);

      // Null or whitespace options are ignored.
      var connectionString = $"host={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};";
      builder = new MySqlConnectionStringBuilder(connectionString);
      builder.SslCa = null;
      builder.SslCert = string.Empty;
      builder.SslKey = "  ";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        Assert.Null(connection.Settings.SslCa);
        Assert.Null(connection.Settings.SslCert);
        Assert.AreEqual("  ", connection.Settings.SslKey);
        connection.Open();
        connection.Close();
      }

      // Failing to provide a value defaults to null.
      connectionString = $"{connectionString}sslca=;sslcert=;sslkey=";
      using (var connection = new MySqlConnection(connectionString))
      {
        Assert.Null(connection.Settings.SslCa);
        Assert.Null(connection.Settings.SslCert);
        Assert.Null(connection.Settings.SslKey);
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCa)), exception.InnerException.Message);
      }

      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCa)), exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = string.Empty;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(string.Format(Resources.FilePathNotSet, nameof(Settings.SslCert)), exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void MissingSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(string.Format(Resources.FilePathNotSet, nameof(Settings.SslKey)), exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCaConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "C:\\certs\\ca.pema";
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslCertConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = "C:\\certs\\client-cert";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void InvalidFileNameForSslKeyConnectionOption()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = "file";
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileNotFound, exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingCaPemCertificate()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void ConnectUsingAllCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyFull;
      builder.TlsVersion = "Tlsv1.2";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }

      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_altered.pem");
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        try { connection.Open(); }
        catch (Exception ex) { Assert.True(ex is MySqlException || ex is AuthenticationException); }
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCaConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslMode = MySqlSslMode.Preferred;

      // Connection attempt is successful since SslMode=Preferred causign SslCa to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void SslCertandKeyConnectionOptionsAreIgnoredOnDifferentSslModes()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = "dummy_file";
      builder.SslCert = null;
      builder.SslKey = " ";
      builder.SslMode = MySqlSslMode.Required;

      // Connection attempt is successful since SslMode=Required causing SslCa, SslCert and SslKey to be ignored.
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        connection.Close();
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWithDummyPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCa.Replace("ca.pem", "ca_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert.Replace("client-cert.pem", "client-cert_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCert;
      builder.SslKey = _sslKey.Replace("client-key.pem", "client-key_dummy.pem");
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileIsNotAKey, exception.InnerException.Message);
      }
    }

    [Test]
    [Property("Category", "Security")]
    public void AttemptConnectionWithSwitchedPemCertificates()
    {
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslCa = _sslCert;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.SslCertificateIsNotCA, exception.InnerException.Message);
      }

      builder.SslCa = _sslKey;
      builder.SslMode = MySqlSslMode.VerifyCA;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileIsNotACertificate, exception.InnerException.Message);
      }

      builder.SslCa = _sslCa;
      builder.SslCert = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.InvalidSslCertificate, exception.InnerException.Message);
      }

      builder.SslCert = _sslCert;
      builder.SslKey = _sslCa;
      builder.SslMode = MySqlSslMode.VerifyFull;
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        var exception = Assert.Throws<MySqlException>(() => connection.Open());
        Assert.AreEqual(Resources.SslConnectionError, exception.Message);
        Assert.AreEqual(Resources.FileIsNotAKey, exception.InnerException.Message);
      }
    }

    #endregion

    #region WL14389

    [Test, Description("Test session variables specific to clients ")]
    [Ignore("This test needs to be executed individually because server setup affects to other tests")]
    public void MaxConnectionsWithPersistAndGlobalVariables()
    {
      var connStr = $"server={Settings.Server};user={Settings.UserID};port={Settings.Port};password={Settings.Password};ssl-mode=required;ConnectionTimeout=5";
      var maxIterations = 7;
      ExecuteSQL("SET GLOBAL max_connections = 10;");
      List<MySqlConnection> connectionList= new();
      using (var conn = new MySqlConnection(connStr))
      {
        while (connectionList.Count()< maxIterations)
        {
          var c1 = new MySqlConnection(connStr);
          c1.Open();
          connectionList.Add(c1);
        }
        Exception ex = Assert.Throws<MySqlException>(() => conn.Open());
        StringAssert.Contains("Too many connections", ex.Message);
      }
      foreach (var item in connectionList)
      {
        item.Close();
        item.Dispose();
      }
      connectionList.Clear();

      ExecuteSQL("SET PERSIST max_connections = 10;");

      using (var conn = new MySqlConnection(connStr))
      {
        while (connectionList.Count() < maxIterations)
        {
          var c1 = new MySqlConnection(connStr);
          c1.Open();
          connectionList.Add(c1);
        }
        Exception ex = Assert.Throws<MySqlException>(() => conn.Open());
        StringAssert.Contains("Too many connections", ex.Message);
      }
      foreach (var item in connectionList)
      {
        item.Close();
        item.Dispose();
      }
      connectionList.Clear();

      ExecuteSQL("SET GLOBAL max_connections = 151", true);
    }

    [Test, Description("CONNECTOR/NET DOESN'T NEED TO RUN SHOW VARIABLES")]
    public void ShowVariablesRemoved()
    {
      using (var dbConn = new MySqlConnection(Settings.ConnectionString))
      {
        dbConn.Open();//Manually verify in server log that show variables is not present
        Assert.AreEqual(ConnectionState.Open, dbConn.State);
      }
    }

    [Test]
    public void ConnectUsingCertificateFileAndTlsVersion()
    {
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      var builder = new MySqlConnectionStringBuilder(Settings.ConnectionString);
      builder.SslMode = MySqlSslMode.Required;
      builder.CertificateFile = "client.pfx";
      builder.CertificatePassword = "pass";
      builder.TlsVersion = "Tlsv1.2";
      using (var connection = new MySqlConnection(builder.ConnectionString))
      {
        connection.Open();
        MySqlCommand cmd = new MySqlCommand("show variables like '%tls_version%'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.Contains("TLSv1", reader.GetString(1));
        }

        cmd = new MySqlCommand("show status like 'Ssl_cipher'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.Contains("AES", reader.GetString(1));
        }

        cmd = new MySqlCommand("show status like 'Ssl_version'", connection);
        using (MySqlDataReader reader = cmd.ExecuteReader())
        {
          Assert.True(reader.Read());
          StringAssert.AreEqualIgnoringCase("TLSv1.2", reader.GetString(1));
        }
      }
    }

    [Test, Description("Classic-Scenario(correct ssl-ca,wrong ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaWrongSslkeySslcertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is only for Windows OS");
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      string[] sslcertlist = new string[] { "", " ", null, "file", "file.pem" };
      string[] sslkeylist = new string[] { "", " ", null, "file", "file.pem" };
      for (int i = 0; i < sslcertlist.Length; i++)
      {

        var connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        connClassic.SslMode = MySqlSslMode.Required;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }

        connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }

        connClassic = new MySqlConnectionStringBuilder();
        connClassic.Server = Host;
        connClassic.Port = Convert.ToUInt32(Port);
        connClassic.UserID = Settings.UserID;
        connClassic.Password = Settings.Password;
        connClassic.SslCert = sslcertlist[i];
        connClassic.SslKey = sslkeylist[i];
        connClassic.SslCa = _sslCa;
        connClassic.SslMode = MySqlSslMode.Prefered;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }

        var conn = new MySqlXConnectionStringBuilder();
        conn.Server = Host;
        conn.Port = Convert.ToUInt32(Port);
        conn.UserID = Settings.UserID;
        conn.Password = Settings.Password;
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = _sslCa;
        conn.SslMode = MySqlSslMode.Required;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }

        conn = new MySqlXConnectionStringBuilder();
        conn.Server = Host;
        conn.Port = Convert.ToUInt32(Port);
        conn.UserID = Settings.UserID;
        conn.Password = Settings.Password;
        conn.SslCert = sslcertlist[i];
        conn.SslKey = sslkeylist[i];
        conn.SslCa = _sslCa;
        using (var c = new MySqlConnection(connClassic.ConnectionString))
        {
          c.Open();
          Assert.AreEqual(ConnectionState.Open, c.State);
        }
      }
    }

    [Test, Description("Clasic-Scenario(correct ssl-ca,no ssl-key/ssl-cert,ssl-mode required and default)")]
    public void CorrectSslcaNoSslkeyorCertRequiredMode()
    {
      if (!Platform.IsWindows()) Assert.Ignore("This test is only for Windows OS");
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      var connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Required;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Prefered;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }

      connClassic = new MySqlConnectionStringBuilder();
      connClassic.Server = Host;
      connClassic.Port = Convert.ToUInt32(Port);
      connClassic.UserID = Settings.UserID;
      connClassic.Password = Settings.Password;
      connClassic.SslCa = _sslCa;
      connClassic.SslMode = MySqlSslMode.Preferred;
      using (var c = new MySqlConnection(connClassic.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }

      var conn = new MySqlXConnectionStringBuilder();
      conn.Server = Host;
      conn.Port = Convert.ToUInt32(Port);
      conn.UserID = Settings.UserID;
      conn.Password = Settings.Password;
      conn.SslCa = _sslCa;
      conn.SslMode = MySqlSslMode.Required;
      using (var c = new MySqlConnection(conn.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }

      conn = new MySqlXConnectionStringBuilder();
      conn.Server = Host;
      conn.Port = Convert.ToUInt32(Port);
      conn.UserID = Settings.UserID;
      conn.Password = Settings.Password;
      conn.SslCa = _sslCa;
      using (var c = new MySqlConnection(conn.ConnectionString))
      {
        c.Open();
        Assert.AreEqual(ConnectionState.Open, c.State);
      }
    }

    [Test, Description("checking different versions of TLS versions")]
    public void SecurityTlsCheck()
    {
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      if (!ServerHaveSsl()) Assert.Ignore("Server doesn't have Ssl support");

      MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull };
      String[] version, ver1Tls;
      var conStr = $"server={Host};port={Port};userid={Settings.UserID};password={Settings.Password};SslCa={_sslCa};SslCert={_sslCert};SslKey={_sslKey};ssl-ca-pwd=pass";
      foreach (MySqlSslMode mode in modes)
      {
        version = new string[] { "TLSv1", "TLSv1.1", "TLSv1.2" };
        foreach (string tlsVersion in version)
        {
          using (var conn = new MySqlConnection(conStr + ";ssl-mode=" + mode + ";tls-version=" + tlsVersion))
          {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.AreEqual(tlsVersion, result);
          }
        }
        version = new string[] { "[TLSv1,TLSv1.1]", "[TLSv1.1,TLSv1.2]", "[TLSv1,TLSv1.2]" };
        ver1Tls = new string[] { "TLSv1.1", "TLSv1.2", "TLSv1.2" };
        for (int i = 0; i < 3; i++)
        {
          using (var conn = new MySqlConnection(conStr + ";ssl-mode=" + mode + ";tls-version=" + version[i]))
          {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.AreEqual(ver1Tls[i], result);
          }
        }
      }
    }

    [Test, Description("checking different versions of TLS versions in old server")]
    public void ServerTlsVersionTest()
    {
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      if (!ServerHaveSsl()) Assert.Ignore("Server doesn't have Ssl support");

      MySqlSslMode[] modes = { MySqlSslMode.Required, MySqlSslMode.VerifyCA, MySqlSslMode.VerifyFull };
      var conStr = $"server={Host};port={Port};userid={Settings.UserID};password={Settings.Password};SslCa={_sslCa};SslCert={_sslCert};SslKey={_sslKey};ssl-ca-pwd=pass";
      foreach (MySqlSslMode mode in modes)
      {
        string[] version = new string[] { "TLSv1", "TLSv1.1", "TLSv1.2" };
        foreach (string tlsVersion in version)
        {
          using (var conn = new MySqlConnection(conStr + $";ssl-mode={mode};tls-version={tlsVersion}"))
          {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.AreEqual(tlsVersion, result);
          }
        }
        version = new string[] { "[TLSv1,TLSv1.1]", "[TLSv1.1,TLSv1.2]", "[TLSv1,TLSv1.2]" };
        var ver1Tls = new string[] { "TLSv1.1", "TLSv1.2", "TLSv1.2" };
        for (int i = 0; i < 3; i++)
        {
          using (var conn = new MySqlConnection(conStr + $";ssl-mode={mode};tls-version={version[i]}"))
          {
            conn.Open();
            MySqlCommand cmd = new MySqlCommand("SELECT variable_value FROM performance_schema.session_status WHERE VARIABLE_NAME='Ssl_version'", conn);
            object result = cmd.ExecuteScalar();
            Assert.AreEqual(ver1Tls[i], result);
          }
        }
      }
    }

    [Test, Description("checking errors when invalid values are used ")]
    public void InvalidTlsversionValues()
    {
      if (Version <= new Version(8, 0, 16)) Assert.Ignore("This test for MySql server 8.0.16 or higher");
      if (!ServerHaveSsl()) Assert.Ignore("Server doesn't have Ssl support");

      string[] version = new string[] { "null", "v1", "[ ]", "[TLSv1.9]", "[TLSv1.1,TLSv1.7]", "ui9" };//blank space is considered as default value
      var conStr = $"server={Host};port={Port};userid={Settings.UserID};password={Settings.Password};SslCa={_sslCa};SslCert={_sslCert};SslKey={_sslKey};ssl-ca-pwd=pass";
      MySqlConnection conn;
      foreach (string tlsVersion in version)
      {
        Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version={tlsVersion}"));
      }

      Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1];tls-version=[TLSv1]"));
      Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.Required};tls-version=[TLSv1.1,TLSv1.2];tls-version=[TLSv1.1,TLSv1.2]"));

      version = new string[] { "[TLSv1]", "[TLSv1.1]", "[TLSv1.2]" };
      string[] version1 = new string[] { "[TLSv1, TLSv1.1]", "[TLSv1, TLSv1.2]", "[TLSv1.1, TLSv1.2]", "[TLSv1, TLSv1.1, TLSv1.2]", "[TLSv1.2, TLSv1.1]" };
      foreach (string tlsVersion in version)
      {
        Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.None};tls-version={tlsVersion}"));
      }
      foreach (string tlsVersion in version1)
      {
        Assert.Throws<ArgumentException>(() => conn = new MySqlConnection(conStr + $";ssl-mode={MySqlSslMode.None};tls-version={tlsVersion}"));
      }
    }

    [Test, Description("Default SSL user with SSL but without SSL Parameters")]
    public void SslUserWithoutSslParams()
    {
      if (!ServerHaveSsl()) Assert.Ignore("Server doesn't have Ssl support");
      MySqlCommand cmd = new MySqlCommand();
      string connstr = $"server={Host};user={Settings.UserID};port={Port};password={Settings.Password};sslmode={MySqlSslMode.None}";
      using (var c = new MySqlConnection(connstr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "SHOW STATUS like 'ssl_cipher'";
        cmd.ExecuteNonQuery();
        var rdr1 = cmd.ExecuteReader();
        while (rdr1.Read())
          Assert.IsNotNull(rdr1.GetValue(1));
      }

      connstr = $"server={Host};user={Settings.UserID};port={Port};password=;sslmode={MySqlSslMode.None}";
      using (var c = new MySqlConnection(connstr))
      {
        Assert.Throws<MySqlException>(() => c.Open());
      }
    }

    [Test]
    public void PositiveSslConnectionWithCertificates()
    {
      if (Version < new Version(5, 7, 0)) Assert.Ignore("This test is for MySql Server 5.7 or higher");
      if (!ServerHaveSsl()) Assert.Ignore("Server doesn't have Ssl support");
      MySqlCommand cmd = new MySqlCommand();

      var connStr = $"server={Host};port={Port};user={Settings.UserID};password={Settings.Password};CertificateFile={_sslCa};CertificatePassword=pass;SSL Mode=Required;";
      using (var c = new MySqlConnection(connStr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "show variables like 'tls_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim().Contains("TLSv1"));
          }
        }

        cmd.CommandText = "show status like 'Ssl_cipher'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim().Contains("AES"));
          }
        }

        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim() == "TLSv1" ||
              rdr.GetValue(1).ToString().Trim() == "TLSv1.1" ||
              rdr.GetValue(1).ToString().Trim() == "TLSv1.2");
          }
        }
      }

      connStr = $"server={Host};port={Port};user={Settings.UserID};password={Settings.Password};sslcert={_sslCert};sslkey={_sslKey};sslca={_sslCa};SSL Mode=VerifyCA;";
      using (var c = new MySqlConnection(connStr))
      {
        c.Open();
        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim().Contains("TLSv1"));
          }
        }

        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_cipher'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim().Contains("AES"));
          }
        }
        cmd.Connection = c;
        cmd.CommandText = "show status like 'Ssl_version'";
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            Assert.True(rdr.GetValue(1).ToString().Trim() == "TLSv1" ||
              rdr.GetValue(1).ToString().Trim() == "TLSv1.1" ||
              rdr.GetValue(1).ToString().Trim() == "TLSv1.2");
          }
        }
      }
    }

    #endregion WL14389

    #region Methods
    public bool ServerHaveSsl()
    {
      Dictionary<string, string> strValues = new();
      
      var commandList = new string[]{ "show variables like 'have_ssl'", "show  status like '%Ssl_version'", "show variables like 'tls_version'" };
      foreach (var item in commandList)
      {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = item;
        using (var rdr = cmd.ExecuteReader())
        {
          while (rdr.Read())
          {
            strValues[rdr.GetString(0)] = rdr.GetString(1);
          }
        }
      }
      return (strValues["have_ssl"] == "YES" && strValues["Ssl_version"].StartsWith("TLS") && !string.IsNullOrEmpty(strValues["tls_version"]))  ? true : false;
    }
    #endregion Methods

  }
}
