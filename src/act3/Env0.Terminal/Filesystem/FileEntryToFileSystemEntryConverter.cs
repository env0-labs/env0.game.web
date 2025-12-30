using System;
using System.Collections.Generic;
using Env0.Terminal.Config.Pocos;
using Env0.Terminal.Filesystem;

namespace Env0.Terminal.Filesystem
{
    /// <summary>
    /// Converts a POCO FileEntry tree (from JSON) into a FileSystemEntry tree for use by FilesystemManager.
    /// </summary>
    public static class FileEntryToFileSystemEntryConverter
    {
        /// <summary>
        /// Recursively converts a FileEntry node and its children to a FileSystemEntry tree.
        /// </summary>
        /// <param name="name">The name of this entry (should match the key in the parent dictionary).</param>
        /// <param name="entry">The FileEntry POCO node.</param>
        /// <param name="parent">Parent FileSystemEntry node, or null if root.</param>
        /// <returns>FileSystemEntry root of the subtree.</returns>
        public static FileEntry Convert(string name, FileEntry entry, FileEntry parent = null)
        {
            var fsEntry = new FileEntry
            {
                Name = name,
                Parent = parent,
                Type = entry.Type
            };

            if (!string.IsNullOrEmpty(entry.Type) && entry.Type.ToLower() == "file")
            {
                // PATCH: Files get their content, no children!
                fsEntry.Content = entry.Content;
                fsEntry.Children = null;
                DebugUtility.PrintContext("Converter", $"File '{name}', Content='{entry.Content ?? "NULL"}'");
            }
            else
            {
                // Directory: clear content, set up children recursively
                fsEntry.Content = null;
                fsEntry.Children = new Dictionary<string, FileEntry>();
                if (entry.Children != null)
                {
                    foreach (var kvp in entry.Children)
                    {
                        var child = Convert(kvp.Key, kvp.Value, fsEntry);
                        fsEntry.Children.Add(kvp.Key, child);
                    }
                }
                DebugUtility.PrintContext("Converter", $"Directory '{name}', ChildCount={fsEntry.Children.Count}");
            }

            return fsEntry;
        }
    }
}
