// Imports
open System.IO
open System.Net.Http
open System.Net.Http.Json

// Arguments for script, skip the filneme
let args : string array = fsi.CommandLineArgs |> Array.tail
let guildId = args[0]
let tabSeparator = '\t' // Used for copypaste, might be \t or ; or ,
let dataPath = Path.Combine(".","data")
let guildFilePath = Path.Combine(dataPath, (sprintf "%s.txt" guildId))

// Full models, but we wont use everything
type Goblin = {
    id : int
    name : string
    btag : string
    skulls : int
    honor : int
    avatar : string
    lastUpdate : int64
}

type Guild = {
    id : int
    name : string 
    member_count : int
    description : string
    members : Goblin list
}

type Data = {
    data : Guild
    token : string 
    link : string
}

let ensureDataDirectory = 
    Directory.CreateDirectory(dataPath) |> ignore

#nowarn "3511" // Disable whining about dynamic compilation
let updateGoblinData = 
    task {
        use client = new HttpClient()
        let url = $"https://api.warcraftrumble.gg/guild/{guildId}"
        let! guildResult = client.GetFromJsonAsync<Data>(url)
        
        let goblins = 
            guildResult.data.members
            |> List.sortBy (fun (gobo : Goblin) -> gobo.name.ToLower()) // Sort by lowercase because of reasons 
            |> List.fold (fun (allGoblinsString : string) (gobo : Goblin) ->
                let goboString = sprintf "%s%c%d%c%d" gobo.name tabSeparator gobo.skulls tabSeparator gobo.honor
                sprintf "%s%s%s" allGoblinsString goboString System.Environment.NewLine
            ) ""
        let file = File.CreateText(guildFilePath)
        file.Write(goblins.ToCharArray())
        file.Close()

        printfn "%s" guildFilePath
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

// Create data directory if it does not exists
ensureDataDirectory
// Fetch goblin data from internets to directory
updateGoblinData 

