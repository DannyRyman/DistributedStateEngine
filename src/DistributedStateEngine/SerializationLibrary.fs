module internal SerializationLibrary

open Wire

open System.IO

let private serializer = new Serializer()

let serializeToByteArray objToSerialize =
  use memoryStream = new MemoryStream()
  serializer.Serialize(objToSerialize, memoryStream)
  memoryStream.ToArray()

let deserializeFromByteArray<'T> (data:byte[]) =
  use memoryStream = new MemoryStream(data)  
  serializer.Deserialize<'T>(memoryStream)