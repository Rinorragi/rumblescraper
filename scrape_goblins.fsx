// Imports
open System.IO
open System.Net.Http
open System.Net.Http.Json
open FSharp.Data

// Arguments for script, skip the filneme
let args : string array = fsi.CommandLineArgs |> Array.tail
let guildIdParam = args[0]
let tabSeparator = '\t' // Used for copypaste, might be \t or ; or ,
let dataPath = Path.Combine(".","data")
let guildFilePath = Path.Combine(dataPath, (sprintf "%s_goblins.txt" guildIdParam))
let leadersFilePath = Path.Combine(dataPath, (sprintf "%s_leaders.txt" guildIdParam))

// Full models, but we wont use everything
type GuildGoblin = {
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
    members : GuildGoblin list
}

type GuildData = {
    data : Guild
    token : string 
    link : string
}

type Goblin = {
    id : int
    name : string
    btag : string
    skulls : int
    honor : int
    level : int 
    pvpWins : int
    leaderAHonor : int
    leaderBHonor : int
    leaderCHonor : int
    amiest : string list list 
}

type GoblinData = {
    data : Goblin
    token : string 
}

let ensureDataDirectory = 
    Directory.CreateDirectory(dataPath) |> ignore

#nowarn "3511" // Disable whining about dynamic compilation
let fetchGoblinGuildData (guildId : int) = 
    task {
        use client = new HttpClient()
        let url = $"https://api.warcraftrumble.gg/guild/{guildId}"
        let! guildResult = client.GetFromJsonAsync<GuildData>(url)
        return guildResult
    }

let fetchGoblinData (goblinId : int) = 
    task {
        use client = new HttpClient()
        let url = $"https://api.warcraftrumble.gg/player/{goblinId}"
        let! goblinResult = client.GetFromJsonAsync<GoblinData>(url) 
        return goblinResult
    }

let updateGoblinData = 
    // Fetch guild info
    let guildData = 
        fetchGoblinGuildData (int guildIdParam)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    
    // Fetch goblin info per each guild member
    let goblinData = 
        guildData.data.members
        |> List.map(fun (gg : GuildGoblin) -> fetchGoblinData(gg.id) |> Async.AwaitTask)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> List.ofArray


    let goblins = 
        goblinData
        |> List.map (fun f -> f.data) // goblins
        |> List.sortBy (fun (gobo : Goblin) -> gobo.name.ToLower()) // Sort by lowercase because of reasons 
        |> List.fold (fun (allGoblinsString : string) (gobo : Goblin) ->
            let honor = gobo.leaderAHonor + gobo.leaderBHonor + gobo.leaderCHonor
            let goboString = sprintf "%s%c%d%c%d%c%d" gobo.name tabSeparator gobo.skulls tabSeparator honor tabSeparator gobo.level
            sprintf "%s%s%s" allGoblinsString goboString System.Environment.NewLine
        ) ""
    let file = File.CreateText(guildFilePath)
    file.Write(goblins.ToCharArray())
    file.Close()

    printfn "%s" guildFilePath

// Create data directory if it does not exists
ensureDataDirectory
// Fetch goblin data from internets to directory
updateGoblinData 

