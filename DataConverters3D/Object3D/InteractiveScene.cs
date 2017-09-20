﻿/*
Copyright (c) 2014, John Lewin, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.PolygonMesh;
using MatterHackers.PolygonMesh.Processors;
using Newtonsoft.Json;

namespace MatterHackers.MeshVisualizer
{
	public class InteractiveScene : Object3D
	{
		public event EventHandler SelectionChanged;

		private IObject3D selectedItem;

		public InteractiveScene()
		{
		}

		[JsonIgnore]
		public IObject3D SelectedItem
		{
			get
			{
				return selectedItem;
			}

			set
			{
				if (selectedItem != value)
				{
					if (SelectedItem?.ItemType == Object3DTypes.SelectionGroup)
					{
						ModifyChildren(ClearSelectionApplyChanges);
					}

					selectedItem = value;
					SelectionChanged?.Invoke(this, null);
				}
			}
		}

		[JsonIgnore]
		public UndoBuffer UndoBuffer { get; } = new UndoBuffer();
		
		[JsonIgnore]
		public bool HasSelection => this.HasChildren() && SelectedItem != null;

		[JsonIgnore]
		public bool ShowSelectionShadow { get; set; } = true;

		public bool IsSelected(Object3DTypes objectType) => HasSelection && SelectedItem.ItemType == objectType;

		public void Save(string mcxPath, string libraryPath, Action<double, string> progress = null)
		{
			var itemsWithUnsavedMeshes = from object3D in this.Descendants()
							  where object3D.Persistable  &&
									object3D.MeshPath == null &&
									object3D.Mesh != null
							  select object3D;

			string assetsDirectory = Path.Combine(libraryPath, "Assets");
			Directory.CreateDirectory(assetsDirectory);

			Dictionary<int, string> assetFiles = new Dictionary<int, string>();

			try
			{
				// Write each unsaved mesh to disk
				foreach (IObject3D item in itemsWithUnsavedMeshes)
				{
					// Calculate the mesh hash
					int hashCode = (int)item.Mesh.GetLongHashCode();

					string assetPath;

					bool savedSuccessfully = true;

					if (!assetFiles.TryGetValue(hashCode, out assetPath))
					{
						// Get an open filename
						string tempStlPath = GetOpenFilePath(libraryPath, ".stl");

						// Save the embedded asset to disk
						savedSuccessfully = MeshFileIo.Save(
							new List<MeshGroup> { new MeshGroup(item.Mesh) },
							tempStlPath,
							new MeshOutputSettings(MeshOutputSettings.OutputType.Binary),
							progress);

						if (savedSuccessfully)
						{
							// There's currently no way to know the actual mesh file hashcode without saving it to disk, thus we save at least once in
							// order to compute the hash but then throw away the duplicate file if an existing copy exists in the assets directory
							string sha1 = MeshFileIo.ComputeSHA1(tempStlPath);
							assetPath = Path.Combine(assetsDirectory, sha1 + ".stl");
							if (!File.Exists(assetPath))
							{
								File.Copy(tempStlPath, assetPath);
							}

							// Remove the temp file
							File.Delete(tempStlPath);

							assetFiles.Add(hashCode, assetPath);
						}
					}

					if (savedSuccessfully && File.Exists(assetPath))
					{
						// Assets should be stored relative to the Asset folder
						item.MeshPath = Path.GetFileName(assetPath);
					}
				}

				// Serialize the scene to disk using a modified Json.net pipeline with custom ContractResolvers and JsonConverters
				File.WriteAllText(mcxPath, this.ToJson());
			}
			catch (Exception ex)
			{
				Trace.WriteLine("Error saving file: ", ex.Message);
			}
		}

		private string GetOpenFilePath(string libraryPath, string extension)
		{
			string filePath;
			do
			{
				filePath = Path.Combine(libraryPath, Path.ChangeExtension(Path.GetRandomFileName(), extension));
			} while (File.Exists(filePath));

			return filePath;
		}

		public void SelectLastChild()
		{
			if (Children.Count > 0)
			{
				SelectedItem = Children.Last();
			}
		}

		public void SelectFirstChild()
		{
			if (Children.Count > 0)
			{
				SelectedItem = Children.First();
			}
		}

		/// <summary>
		/// Provides a safe context to manipulate Scene.Children. Copies Scene.Children into a new list, invokes the 'modifier'
		/// Action passing in the copied list and swaps Scene.Children to the new list after the Action has a chance to modify the tree
		/// </summary>
		/// <param name="modifier">The Action to invoke</param>
		public void ModifyChildren(Action<List<IObject3D>> modifier)
		{
			// Copy the child items
			var clonedChildren = new List<IObject3D>(Children);

			// Pass them to the action
			modifier(clonedChildren);

			// Swap the modified list into place
			Children = clonedChildren;
		}

		private void ClearSelectionApplyChanges(List<IObject3D> target)
		{
			SelectedItem.CollapseInto(target);
		}

		public void ClearSelection()
		{
			if (HasSelection)
			{
				SelectedItem = null;
			}
		}

		public void AddToSelection(IObject3D itemToAdd)
		{
			if (itemToAdd == SelectedItem || SelectedItem?.Children?.Contains(itemToAdd) == true)
			{
				return;
			}

			if (HasSelection)
			{
				if(SelectedItem.ItemType == Object3DTypes.SelectionGroup)
				{
					this.Children.Remove(itemToAdd);
					SelectedItem.Children.Add(itemToAdd);
				}
				else // add a new selection group and add to its children
				{
					// We're adding a new item to the selection. To do so we wrap the selected item
					// in a new group and with the new item. The selection will continue to grow in this
					// way until it's applied, due to a loss of focus or until a group operation occurs
					var newSelectionGroup = new Object3D
					{
						ItemType = Object3DTypes.SelectionGroup,
					};

					newSelectionGroup.Children.Add(SelectedItem);
					newSelectionGroup.Children.Add(itemToAdd);

					this.Children.Remove(itemToAdd);
					this.Children.Remove(SelectedItem);
					this.Children.Add(newSelectionGroup);

					SelectedItem = newSelectionGroup;
				}
			}
			else if (Children.Contains(itemToAdd))
			{
				SelectedItem = itemToAdd;
			}
			else
			{
				throw new Exception("Unable to select external object. Item must be in the scene to be selected.");
			}
		}

		public void Load(string mcxPath)
		{
			var root = Object3D.Load(mcxPath, CancellationToken.None);

			this.ModifyChildren((children) =>
			{
				children.Clear();

				if (root != null)
				{
					children.AddRange(root.Children);
				}
			});
		}
	}
}
