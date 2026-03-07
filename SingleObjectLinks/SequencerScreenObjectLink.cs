using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_LevelEditor.SingleObjectLinks
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class SequencerScreenObjectLink : SingleObjectLink
    {
        public override LE_Object.ObjectType? targetObjectType => LE_Object.ObjectType.SEQUENCE;
    }
}
