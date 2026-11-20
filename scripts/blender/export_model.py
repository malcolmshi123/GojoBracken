import bpy
import hashlib
import json
import runpy
import sys
from pathlib import Path
from mathutils import Matrix

repo=Path(__file__).resolve().parents[2]
root=repo/'artifacts/blender'
assert Path(bpy.data.filepath).resolve()==(root/'Gojo.blend').resolve()
runpy.run_path(str(root/'scripts/validate_model.py'),run_name='__main__')
rig=bpy.data.objects['Gojo Rig']
bones=list(rig.data.bones)
indices={bone.name:i for i,bone in enumerate(bones)}
conversion=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
def point(value):return list((conversion@value.to_4d()).to_3d())
def matrix(value):
    result=conversion@value@conversion.inverted()
    return [x for row in result for x in row]
document={'formatVersion':1,'sourceSha256':hashlib.sha256((root/'Gojo.blend').read_bytes()).hexdigest(),'bones':[], 'meshes':[]}
for bone in bones:
    document['bones'].append({'name':bone.name,'parent':indices[bone.parent.name] if bone.parent else -1,
        'rest':matrix(rig.matrix_world@bone.matrix_local),'pose':matrix(rig.matrix_world@rig.pose.bones[bone.name].matrix)})
for obj in bpy.data.collections['Gojo'].objects:
    if obj.type!='MESH':continue
    mesh=obj.data
    assert len(mesh.materials)==1
    mesh.calc_loop_triangles()
    ev=obj.evaluated_get(bpy.context.evaluated_depsgraph_get());posed=ev.to_mesh()
    texture=next(node.image.name for node in mesh.materials[0].node_tree.nodes if node.type=='TEX_IMAGE' and node.image)
    item={'name':obj.name,'texture':texture,'positions':[],'normals':[],'uv':[],'triangles':[],'boneIndices':[],'weights':[],'bonesPerVertex':[],'posed':[]}
    lookup={}
    normal_matrix=obj.matrix_world.to_3x3().inverted().transposed()
    for triangle in mesh.loop_triangles:
        triangle_indices=[]
        for loop_index in triangle.loops:
            vertex_index=mesh.loops[loop_index].vertex_index
            uv=mesh.uv_layers.active.data[loop_index].uv
            normal=posed.corner_normals[loop_index].vector
            key=(vertex_index,*uv,*normal)
            if key not in lookup:
                lookup[key]=len(item['positions'])//3
                vertex=mesh.vertices[vertex_index]
                item['positions'].extend(point(obj.matrix_world@vertex.co))
                item['posed'].extend(point(obj.matrix_world@posed.vertices[vertex_index].co))
                n=normal_matrix@normal;n.normalize()
                item['normals'].extend((n.x,n.z,n.y))
                item['uv'].extend(uv)
                weights=[(indices[obj.vertex_groups[g.group].name],g.weight) for g in vertex.groups if g.weight>0 and obj.vertex_groups[g.group].name in indices]
                assert weights and abs(sum(w for _,w in weights)-1)<.00001,(obj.name,vertex_index,weights)
                weights.sort(key=lambda pair:pair[1],reverse=True)
                item['bonesPerVertex'].append(len(weights))
                item['boneIndices'].extend(i for i,_ in weights)
                item['weights'].extend(w for _,w in weights)
            triangle_indices.append(lookup[key])
        item['triangles'].extend(reversed(triangle_indices))
    ev.to_mesh_clear()
    document['meshes'].append(item)
output=repo/'artifacts/unity/Gojo.json'
output.parent.mkdir(exist_ok=True)
output.write_text(json.dumps(document,separators=(',',':')))
print('GOJO UNITY EXPORT:',len(document['bones']),'bones;',len(document['meshes']),'meshes;',sum(len(m['positions'])//3 for m in document['meshes']),'vertices;',output)
