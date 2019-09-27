using System;
using System.Text;

namespace ProclickEmployeeLib.AutoUpdater
{
    public class VersionNumber:IComparable<VersionNumber>
    {
        #region PUBLIC METHODS

        public int CompareTo(VersionNumber other)
        {
            /* Can only compare version numbers of the same format. */
            if (other._components.Length != _components.Length)
            {
                throw new ArgumentException("The two version numbers are not of the same format.", "other");
            }

            for (int i = 0; i < _components.Length; i++)
            {
                if (other._components[i] < _components[i])
                {
                    return 1;
                }

                if (other._components[i] > _components[i])
                {
                    return -1;
                }
            }

            return 0;
        }

        /// <summary>
        /// Converts the version number to a string, using the pre-cached string
        /// if the method has been called previously.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_text == String.Empty)
            {
                bool first = true;
                StringBuilder sb = new StringBuilder();

                foreach (int comp in _components)
                {
                    if (!first)
                    {
                        sb.Append(".");
                    }
                    else
                    {
                        first = false;
                    }

                    sb.Append(comp);
                }

                _text = sb.ToString();
            }

            return _text;
        }

        #endregion

        #region CONSTRUCTION / DISPOSAL

        /// <summary>
        /// Constructor. Private use only - use VersionNumber.Parse.
        /// </summary>
        /// <param name="components"></param>
        private VersionNumber(int[] components)
        {
            _components = components;
            _text = String.Empty;
        }

        #endregion

        #region STATICS

        /// <summary>
        /// Creates a new VersionNumber object from a given string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static VersionNumber Parse(string s)
        {
            string[] toks = s.Split('.');
            int[] components = new int[toks.Length];

            for (int i = 0; i < toks.Length; i++)
            {
                components[i] = Int32.Parse(toks[i]);
            }

            return new VersionNumber(components);
        }

        #endregion

        #region ATTRIBUTES

        private int[] _components;
        private string _text;

        #endregion
    }
}
