﻿using System;
using System.Runtime.Serialization;
using System.Security;
using System.Xml.Serialization;
using EasyConnect.Common;

namespace EasyConnect.Protocols
{
    /// <summary>
    /// Base class for all connections, contains common properties for host name, password, GUID, and name.
    /// </summary>
    [Serializable]
    public abstract class BaseConnection : IConnection
    {
        /// <summary>
        /// Pointer to the folder that contains the bookmark of this connection.  Not serialized because of the fact that this data is implicit in the 
        /// hierarchical folder structure for bookmarks.
        /// </summary>
        [XmlIgnore]
        [NonSerialized]
        protected BookmarksFolder _parentFolder = null;

        /// <summary>
        /// Password, if any, used when establishing this connection.  Not serialized because we instead serialize an encrypted, Base64-encoded version of
        /// this value.
        /// </summary>
        [NonSerialized]
        protected SecureString _password = null;

        /// <summary>
        /// Default constructor that initializes <see cref="Guid"/>.
        /// </summary>
        protected BaseConnection()
        {
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Serialization constructor that initializes the properties from the serialization data.
        /// </summary>
        /// <param name="info">Serialization data to read from.</param>
        /// <param name="context">Streaming context to use during deserialization.</param>
        protected BaseConnection(SerializationInfo info, StreamingContext context)
        {
            IsBookmark = info.GetBoolean("IsBookmark");
            Name = info.GetString("Name");
            Host = info.GetString("Host");
            Guid = new Guid(info.GetString("Guid"));
            string encryptedPassword = info.GetString("Password");

            if (!String.IsNullOrEmpty(encryptedPassword))
                Base64Password = encryptedPassword;

            if (Guid == Guid.Empty)
                Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Host name of the server that we are to connect to.
        /// </summary>
        public string Host
        {
            get;
            set;
        }

        /// <summary>
        /// Display text to use when identifying this connection:  <see cref="Name"/> if it's not null, <see cref="Host"/> otherwise.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return String.IsNullOrEmpty(Name)
                           ? Host
                           : Name;
            }
        }

        /// <summary>
        /// Name used to identify this connection for bookmarking purposes.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Pointer to the folder that contains the bookmark of this connection.  Not serialized because of the fact that this data is implicit in the 
        /// hierarchical folder structure for bookmarks.
        /// </summary>
        [XmlIgnore]
        public virtual BookmarksFolder ParentFolder
        {
            get
            {
                return _parentFolder;
            }

            set
            {
                _parentFolder = value;
            }
        }

        /// <summary>
        /// Unique identifier for this connection.
        /// </summary>
        public Guid Guid
        {
            get;
            set;
        }

        /// <summary>
        /// Encrypted and Base64-encoded data in <see cref="_password"/>
        /// </summary>
        [XmlElement("Password")]
        public string Base64Password
        {
            get
            {
                if (Password == null)
                    return null;

                return Convert.ToBase64String(CryptoUtilities.Encrypt(ConnectionFactory.EncryptionPassword, _password));
            }

            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    Password = null;
                    return;
                }

                // Decrypt the password and put it into a secure string
                byte[] decryptedPassword = CryptoUtilities.Decrypt(ConnectionFactory.EncryptionPassword, Convert.FromBase64String(value));
                SecureString password = new SecureString();

                for (int i = 0; i < decryptedPassword.Length; i++)
                {
                    if (decryptedPassword[i] == 0)
                        break;

                    password.AppendChar((char) decryptedPassword[i]);
                    decryptedPassword[i] = 0;
                }

                Password = password;
            }
        }

        /// <summary>
        /// Password, if any, used when establishing this connection.  Not serialized because we instead serialize an encrypted, Base64-encoded version of
        /// this value.
        /// </summary>
        [XmlIgnore]
        public SecureString Password
        {
            get
            {
                return _password;
            }

            set
            {
                _password = value;

                if (_password != null)
                    _password.MakeReadOnly();
            }
        }

        /// <summary>
        /// Flag indicating whether or not this connection is part of a bookmark.
        /// </summary>
        public bool IsBookmark
        {
            get;
            set;
        }

        /// <summary>
        /// Serializes the contents of the connection.
        /// </summary>
        /// <param name="info">Store where we are to serialize the data.</param>
        /// <param name="context">Streaming context to use during the serialization process.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(
                "Password", _password == null
                                ? null
                                : Convert.ToBase64String(CryptoUtilities.Encrypt(ConnectionFactory.EncryptionPassword, _password)));
            info.AddValue("IsBookmark", IsBookmark);
            info.AddValue("Name", Name);
            info.AddValue("Host", Host);
            info.AddValue("Guid", Guid.ToString());
        }

        /// <summary>
        /// Creates a copy of this connection object with a new <see cref="Guid"/> value.
        /// </summary>
        /// <returns>A new copy of this connection object with its <see cref="Guid"/> property initialized to a new value.</returns>
        public virtual object Clone()
        {
            object clonedConnection = SerializationUtilities.Clone(this);

            ((BaseConnection) clonedConnection).ParentFolder = null;
            ((BaseConnection) clonedConnection).Guid = new Guid();

            return clonedConnection;
        }

        /// <summary>
        /// Creates an anonymized copy of this connection, with sensitive information, like <see cref="Password"/>, removed.
        /// </summary>
        /// <returns>An anonymized copy of this connection, with sensitive information, like <see cref="Password"/>, removed.</returns>
        public virtual object CloneAnon()
        {
            BaseConnection clonedConnection = (BaseConnection) Clone();
            clonedConnection.Password = null;

            return clonedConnection;
        }
    }
}