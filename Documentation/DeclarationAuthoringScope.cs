using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.IO;

namespace NPLForVisualStudio
{
    /// <summary>
    /// Provides the information for word completion and member selection to CodeSense in response to a
    /// parse request.
    /// </summary>
	public class DeclarationAuthoringScope
	{
		private string qualifiedName = String.Empty;
        public string m_quickInfoText;
        public TextSpan m_quickInfoSpan = new TextSpan();
        public string m_goto_filename = null;
        public TextSpan m_goto_textspan = new TextSpan();
        

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthoringScope"/> class.
		/// </summary>
		public DeclarationAuthoringScope()
		{
		}

		
		/// <summary>
		/// Sets the name of the qualified.
		/// </summary>
		/// <param name="qualifiedName">Name of the qualified.</param>
		public void SetQualifiedName(string qualifiedName)
		{
			if (qualifiedName == null)
				throw new ArgumentNullException("qualifiedName");

			this.qualifiedName = qualifiedName;
		}


        /// <summary>
        /// Defines methods to support the comparison of objects for equality.
        /// </summary>
        private class DeclarationEqualityComparer : IEqualityComparer<Declaration>
		{
			/// <summary>
			/// Determines whether the specified objects are equal.
			/// </summary>
			/// <param name="x">The first object of type <paramref name="T"/> to compare.</param>
			/// <param name="y">The second object of type <paramref name="T"/> to compare.</param>
			/// <returns>
			/// true if the specified objects are equal; otherwise, false.
			/// </returns>
			public bool Equals(Declaration x, Declaration y)
			{
				return string.Compare(x.Name, y.Name, StringComparison.Ordinal) == 0 &&
				       x.DeclarationType == y.DeclarationType;
			}

			/// <summary>
			/// Returns a hash code for the specified object.
			/// </summary>
			/// <param name="obj">The <see cref="T:System.Object"/> for which a hash code is to be returned.</param>
			/// <returns>A hash code for the specified object.</returns>
			/// <exception cref="T:System.ArgumentNullException">The type of <paramref name="obj"/> is a reference type and <paramref name="obj"/> is null.</exception>
			public int GetHashCode(Declaration obj)
			{
				if(obj!=null)
					return obj.GetHashCode();

				return 0;
			}
		}
	}
}