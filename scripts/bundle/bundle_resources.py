import argparse
import hashlib
import json
from pathlib import Path
import UnityPy

def audio_payloads(bundle):
    result={}
    for alias,obj in bundle.container.items():
        if obj.type.name!='AudioClip':continue
        clip=obj.read_typetree()
        resource=clip['m_Resource']
        name=resource['m_Source'].replace('\\','/').rsplit('/',1)[-1]
        reader=bundle.file.files[name]
        reader.Position=resource['m_Offset']
        result[alias]=reader.read_bytes(resource['m_Size'])
    return result

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('source',type=Path)
    parser.add_argument('--extract',type=Path)
    parser.add_argument('--compare',type=Path)
    args=parser.parse_args()
    source=UnityPy.load(str(args.source))
    if args.extract:
        args.extract.mkdir(parents=True,exist_ok=True)
        resources=[]
        for name,reader in source.file.files.items():
            if not name.endswith(('.resource','.resS')):continue
            target='Audio.resource' if name.endswith('.resource') else 'Textures.resS'
            reader.Position=0
            payload=reader.read_bytes(reader.Length)
            (args.extract/target).write_bytes(payload)
            resources.append({'source':name,'file':target,'sha256':hashlib.sha256(payload).hexdigest()})
        (args.extract/'resources.json').write_text(json.dumps(resources,indent=2))
        textures=[]
        for obj in source.objects:
            if obj.type.name!='Texture2D':continue
            texture=obj.read_typetree()
            stream=texture['m_StreamData']
            if stream['size']:
                name=stream['path'].replace('\\','/').rsplit('/',1)[-1]
                reader=source.file.files[name];reader.Position=stream['offset']
                payload=reader.read_bytes(stream['size'])
            else:payload=bytes(texture['image data'])
            filename='Texture'+str(len(textures))+'.bin'
            (args.extract/filename).write_bytes(payload)
            textures.append({'name':texture['m_Name'],'file':filename,'sha256':hashlib.sha256(payload).hexdigest()})
        (args.extract/'textures.json').write_text(json.dumps({'items':textures},indent=2))
        print('RELEASE RESOURCES EXTRACTED UNCHANGED',len(resources))
    if args.compare:
        actual=audio_payloads(UnityPy.load(str(args.compare)))
        expected=audio_payloads(source)
        if set(expected)!=set(actual):raise RuntimeError('Audio clip set changed')
        records=[]
        for name,data in expected.items():
            if actual[name]!=data:raise RuntimeError('Encoded audio changed: '+name)
            records.append({'name':name,'encoded_bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
        (args.compare.parent/'encoded-audio-verification.json').write_text(json.dumps(records,indent=2))
        print('ENCODED AUDIO MATCHES RELEASE BYTE FOR BYTE',json.dumps(records))

if __name__=='__main__':main()
