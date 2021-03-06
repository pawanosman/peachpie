﻿using Pchp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Peachpie.Library.PDO
{
    partial class PDO
    {
        /// <summary>
        /// Lazily initialized set of explicitly set attributes.
        /// If not specified, the attributed has its default value.
        /// </summary>
        private protected Dictionary<PDO_ATTR, PhpValue> _lazyAttributes;

        private protected bool TryGetAttribute(PDO_ATTR attribute, out PhpValue value)
        {
            if (_lazyAttributes != null && _lazyAttributes.TryGetValue(attribute, out value))
            {
                return true;
            }

            // default values:
            switch (attribute)
            {
                case PDO_ATTR.ATTR_DRIVER_NAME: value = Driver.Name; return true;
                case PDO_ATTR.ATTR_SERVER_VERSION: value = Connection.ServerVersion; return true;
                case PDO_ATTR.ATTR_CLIENT_VERSION: value = Driver.ClientVersion; return true;

                case PDO_ATTR.ATTR_AUTOCOMMIT: value = PhpValue.True; return true;
                case PDO_ATTR.ATTR_PREFETCH: value = 0; return true;
                case PDO_ATTR.ATTR_TIMEOUT: value = 30; return true;
                case PDO_ATTR.ATTR_ERRMODE: value = ERRMODE_SILENT; return true;
                case PDO_ATTR.ATTR_SERVER_INFO: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CONNECTION_STATUS: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CASE: value = (int)PDO_CASE.CASE_LOWER; return true;
                case PDO_ATTR.ATTR_CURSOR_NAME: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_CURSOR: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_ORACLE_NULLS: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_PERSISTENT: value = PhpValue.False; return true;
                case PDO_ATTR.ATTR_STATEMENT_CLASS: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_STRINGIFY_FETCHES: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN: value = PhpValue.Null; return true;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE: value = 0; return true;
                case PDO_ATTR.ATTR_EMULATE_PREPARES: value = PhpValue.False; return true;

                default:
                    // driver specific:
                    if (attribute > PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                    {
                        value = Driver.GetAttribute(this, attribute);
                        return Operators.IsSet(value);
                    }

                    //TODO : what to do on unknown attribute ?
                    value = PhpValue.Null;
                    return false;
            }
        }

        /// <summary>
        /// Retrieve a database connection attribute
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>A successful call returns the value of the requested PDO attribute. An unsuccessful call returns <c>null</c>.</returns>
        public virtual PhpValue getAttribute(PDO_ATTR attribute) => TryGetAttribute(attribute, out var value) ? value : PhpValue.Null;

        /// <summary>
        /// Set an attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public virtual bool setAttribute(PDO_ATTR attribute, PhpValue value)
        {
            if (_lazyAttributes == null)
            {
                _lazyAttributes = new Dictionary<PDO_ATTR, PhpValue>();
            }

            long l; // temp value

            switch (attribute)
            {
                //readonly
                case PDO_ATTR.ATTR_SERVER_INFO:
                case PDO_ATTR.ATTR_SERVER_VERSION:
                case PDO_ATTR.ATTR_CLIENT_VERSION:
                case PDO_ATTR.ATTR_CONNECTION_STATUS:
                case PDO_ATTR.ATTR_DRIVER_NAME:
                    return false;

                //boolean

                case PDO_ATTR.ATTR_AUTOCOMMIT:
                case PDO_ATTR.ATTR_EMULATE_PREPARES:
                    _lazyAttributes[attribute] = value;
                    return true;

                //strict positif integers

                case PDO_ATTR.ATTR_PREFETCH:
                case PDO_ATTR.ATTR_TIMEOUT:
                    _lazyAttributes[attribute] = value;
                    return true;

                //remaining

                case PDO_ATTR.ATTR_ERRMODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_ERRMODE), (int)l))
                    {
                        _lazyAttributes[attribute] = l;
                        return true;
                    }
                    else
                    {
                        // Warning: PDO::setAttribute(): SQLSTATE[HY000]: General error: invalid error mode
                        PhpException.InvalidArgument(nameof(value));
                        return false;
                    }
                case PDO_ATTR.ATTR_CASE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CASE), (int)l))
                    {
                        _lazyAttributes[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_CURSOR:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_CURSOR), (int)l))
                    {
                        _lazyAttributes[attribute] = l;
                        return true;
                    }
                    return false;
                case PDO_ATTR.ATTR_DEFAULT_FETCH_MODE:
                    l = value.ToLong();
                    if (Enum.IsDefined(typeof(PDO_FETCH), (int)l))
                    {
                        _lazyAttributes[attribute] = l;
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_STATEMENT_CLASS:
                    if (value.IsPhpArray(out var arr) && arr.Count != 0)
                    {
                        _lazyAttributes[attribute] = arr.DeepCopy();
                        return true;
                    }
                    return false;

                case PDO_ATTR.ATTR_FETCH_CATALOG_NAMES:
                case PDO_ATTR.ATTR_FETCH_TABLE_NAMES:
                case PDO_ATTR.ATTR_MAX_COLUMN_LEN:
                case PDO_ATTR.ATTR_ORACLE_NULLS:
                case PDO_ATTR.ATTR_PERSISTENT:
                case PDO_ATTR.ATTR_STRINGIFY_FETCHES:
                    throw new NotImplementedException();

                //statement only
                case PDO_ATTR.ATTR_CURSOR_NAME:
                    return false;

                default:

                    // driver specific
                    try
                    {
                        if (attribute >= PDO_ATTR.ATTR_DRIVER_SPECIFIC)
                        {
                            return Driver.TrySetAttribute(_lazyAttributes, attribute, value);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        this.HandleError(ex);
                        return false;
                    }

                    // invalid attribute:
                    Debug.WriteLine($"PDO_ATTR {attribute.ToString()} is not known.");
                    return false;
            }

        }
    }
}
