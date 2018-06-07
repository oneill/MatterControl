﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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

using MatterHackers.Agg.VertexSource;
using MatterHackers.DataConverters3D;
using Newtonsoft.Json;
using System.Linq;

namespace MatterHackers.MatterControl.DesignTools
{
	using MatterHackers.Agg.UI;
	using MatterHackers.Localizations;

	public class LinearExtrude : Object3D, IPublicPropertyObject
	{
		public double Height { get; set; } = 5;

		[JsonIgnore]
		private IVertexSource VertexSource
		{
			get
			{
				var item = this.Descendants().Where((d) => d is IPathObject).FirstOrDefault();
				if (item is IPathObject pathItem)
				{
					return pathItem.VertexSource;
				}

				return null;
			}
		}

		public LinearExtrude()
		{
			Name = "Linear Extrude".Localize();
		}

		public override void OnInvalidate(InvalidateArgs invalidateType)
		{
			if ((invalidateType.InvalidateType.HasFlag(InvalidateType.Content)
				|| invalidateType.InvalidateType.HasFlag(InvalidateType.Matrix)
				|| invalidateType.InvalidateType.HasFlag(InvalidateType.Path))
				&& invalidateType.Source != this
				&& !RebuildSuspended)
			{
				Rebuild(null);
			}
			else
			{
				base.OnInvalidate(invalidateType);
			}
		}

		public override void Rebuild(UndoBuffer undoBuffer)
		{
			SuspendRebuild();
			Mesh = VertexSourceToMesh.Extrude(this.VertexSource, Height);
			ResumeRebuild();

			Invalidate(new InvalidateArgs(this, InvalidateType.Mesh));
		}
	}
}