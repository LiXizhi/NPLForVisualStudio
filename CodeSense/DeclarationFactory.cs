using System;

namespace NPLForVisualStudio
{
    /// <summary>
    /// Creates declarations based on AST nodes.
    /// </summary>
    public static class DeclarationFactory
    {
        /// <summary>
        /// Creates a declaration for an Identifier node.
        /// </summary>
        /// <param name="declarationType">The type of the declaration.</param>
        /// <param name="identifier">The Identifier node to use.</param>
        /// <returns>An instance of the <see cref="Declaration"/> class.</returns>
        public static Declaration CreateDeclaration(DeclarationType declarationType, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return new Declaration
                    {
                        DeclarationType = declarationType,
                        Name = identifier
                    };
        }

        /// <summary>
        /// Creates a declaration for an Identifier node.
        /// </summary>
        /// <param name="declarationType">The type of the declaration.</param>
        /// <param name="type">The type of the declared variable/field.</param>
        /// <param name="identifier">The Identifier node to use.</param>
        /// <returns>An instance of the <see cref="Declaration"/> class.</returns>
        public static Declaration CreateDeclaration(DeclarationType declarationType, bool isLocal, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return new Declaration
                        {
                            DeclarationType = declarationType,
                            IsLocal = isLocal,
                            Name = identifier
                        };
        }

        /// <summary>
        /// Creates a declaration for an Identifier node.
        /// </summary>
        /// <param name="declarationType">The type of the declaration.</param>
        /// <param name="type">The type of the declared variable/field.</param>
        /// <param name="identifier">The Identifier node to use.</param>
        /// <returns>An instance of the <see cref="Declaration"/> class.</returns>
        public static Declaration CreateDeclaration(DeclarationType declarationType, string type, string identifier)
        {
            if (identifier == null)
                throw new ArgumentNullException("identifier");

            return new Declaration
            {
                DeclarationType = declarationType,
                Type = type,
                Name = identifier
            };
        }
    }
}
