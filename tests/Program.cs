using ScheduleIStats;
int checks=0;
void Assert(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); checks++; }
Assert(DisplayFormat.Money(1234.49m)=="$1,234","game dollars without regional culture");
Assert(DisplayFormat.Money(0m)=="$0","zero money formatting");
Sale NewSale(string id="one") => new() {Id=id,Day=1,Time=1200,Total=100,Lines=new(){new(){ProductId="og",Product="OG",Quantity=5}}};
var data=new LedgerData();
Assert(Ledger.Append(data,NewSale()),"accept sale");
Assert(!Ledger.Append(data,NewSale()),"deduplicate receipt");
Assert(data.Sales.Count==1 && data.Sales[0].Sequence==1,"continuous sequence");
var invalid=NewSale("bad"); invalid.Lines[0].Quantity=0;
Assert(!Ledger.Append(data,invalid),"reject invalid quantity");
invalid=NewSale("bad"); invalid.Total=-1;
Assert(!Ledger.Append(data,invalid),"reject negative payment");
Assert(Ledger.Append(data,NewSale("two")) && data.Sales[1].Sequence==2,"next sequence");
string folder=Path.Combine(Path.GetTempPath(),"ScheduleIStats-tests-"+Guid.NewGuid().ToString("N"));
string path=Path.Combine(folder,"historial.json");
Ledger.Save(path,data);
Assert(Ledger.Load(path).Sales.Count==2,"save and reload");
Ledger.Save(path,data);
Assert(File.Exists(path+".bak"),"preserve previous snapshot");
File.WriteAllText(path,"broken");
bool rejected=false; try{Ledger.Load(path);}catch{rejected=true;}
Assert(rejected && File.ReadAllText(path)=="broken","corrupt history fails without overwriting");
Assert(Ledger.Load(path+".bak").Sales.Count==2,"backup remains readable");
Assert(Ledger.Key("save1")!=Ledger.Key("save2"),"separate save identities");
Assert(Ledger.Minute(2,100)<Ledger.Minute(2,1200),"rollback time comparison");
var empty = new Packet { Kind="batch", LedgerId=Guid.NewGuid().ToString("N"), Count=0, After=0 };
var replica = Replica.Apply(null,empty);
Assert(replica != null && replica.Sales.Count==0,"empty host ledger can synchronize");
var first=NewSale("network-one"); first.Sequence=1;
var packet=new Packet {Kind="batch",LedgerId=empty.LedgerId,Count=1,After=0,Sales=new(){first}};
replica=Replica.Apply(replica,packet);
Assert(replica?.Sales.Count==1,"first network batch");
Assert(Replica.Apply(replica,packet)==null,"reject repeated network batch");
var second=NewSale("network-two"); second.Sequence=2;
packet.After=1; packet.Count=2; packet.Sales=new(){second};
var next=Replica.Apply(replica,packet);
Assert(next?.Sales.Count==2 && replica!.Sales.Count==1,"incremental batch preserves previous snapshot");
packet.After=3;
Assert(Replica.Apply(replica,packet)==null,"reject sequence gap");
packet.After=1; packet.Sales=new(){first};
Assert(Replica.Apply(replica,packet)==null,"reject duplicate receipt and wrong sequence");
packet.Sales=new(){second}; packet.Protocol=1;
Assert(Replica.Apply(replica,packet)==null,"reject old network protocol");
packet.Protocol=4; packet.Count=0;
Assert(Replica.Apply(replica,packet)==null,"reject inconsistent total");
var different=new Packet {Kind="batch",LedgerId=Guid.NewGuid().ToString("N"),Count=0,After=0};
Assert(Replica.Apply(replica,different)?.Sales.Count==0,"switch save without merging histories");
Assert(replica!.Sales.Count==1,"rejected packets do not mutate local history");
var stock=StockTotals.Create(new[]{new StockLine{ProductId="og",Product="OG",Location="Casa / Estante",Quantity=5},
    new StockLine{ProductId="og",Product="OG",Location="Casa / Estante",Quantity=10},
    new StockLine{ProductId="og",Product="OG",Location="Depósito / Estante",Quantity=2}},DateTime.UtcNow);
Assert(stock.Lines.Count==2 && stock.Lines.Sum(l=>l.Quantity)==17,"stock aggregates by product and location");
var sameStock=StockTotals.Create(stock.Lines.AsEnumerable().Reverse(),DateTime.UtcNow.AddSeconds(5));
Assert(sameStock.Revision==stock.Revision,"stable stock revision across scans and ordering");
var sr=new StockReplica();
var sp=new Packet{StockRevision=stock.Revision,StockCount=2,StockAfter=0,StockCheckedUtc=stock.CheckedUtc,StockLines=stock.Lines.Take(1).ToList()};
Assert(sr.Apply(sp)==null && sr.Offset==1,"partial stock remains unpublished");
sp.StockAfter=1; sp.StockLines=stock.Lines.Skip(1).ToList();
Assert(sr.Apply(sp)?.Lines.Sum(l=>l.Quantity)==17,"stock pages assemble complete snapshot");
Assert(sr.Apply(sp)==null && sr.Offset==2,"stock page replay is rejected");
var emptyStock=StockTotals.Create(Array.Empty<StockLine>(),DateTime.UtcNow);
sp=new Packet{StockRevision=emptyStock.Revision,StockCount=0,StockAfter=0,StockCheckedUtc=emptyStock.CheckedUtc};
Assert(sr.Apply(sp)?.Lines.Count==0,"empty stock replaces previous inventory");
sp.StockCount=1; sp.StockLines=new(){new StockLine{ProductId="bad",Product="Bad",Location="Casa",Quantity=-1}};
Assert(sr.Apply(sp)==null,"reject negative stock quantity");
var places=StockPlaces.Pages(new[]{
    new StockLine{ProductId="og",Product="OG",Location="Casa / Estante A",Quantity=5},
    new StockLine{ProductId="og",Product="OG",Location="Casa / Estante B",Quantity=10},
    new StockLine{ProductId="og",Product="OG",Location="Depósito / Estante A",Quantity=2}});
Assert(places.Count==2 && places[0].Place=="Casa" && places[0].Items.Single().Quantity==15,"combine containers within the same place");
Assert(places.Sum(p=>p.Items.Sum(l=>l.Quantity))==17,"place grouping preserves stock totals");
Assert(StockPlaces.Pages(Array.Empty<StockLine>()).Count==0,"empty stock has no place pages");
var playerStock=StockTotals.Create(new[]{
    new StockLine{ProductId="og",Product="OG",Location="Casa / Estante",Quantity=5},
    new StockLine{ProductId="og",Product="OG",Location="Jugador: Bruno",Quantity=8},
    new StockLine{Location="Jugador: Otro / nombre",EmptyPlace=true}},DateTime.UtcNow);
var playerPages=StockPlaces.Pages(playerStock.Lines);
Assert(playerPages.Count==3 && playerPages.Single(p=>p.Place=="Jugador: Bruno").Total==8,"players remain separate from property stock");
Assert(playerPages.Single(p=>p.Place=="Jugador: Otro / nombre").Items.Count==0,"empty player appears as a place and keeps full name");
var pr=new StockReplica();
var pp=new Packet{StockRevision=playerStock.Revision,StockCount=playerStock.Lines.Count,StockAfter=0,StockCheckedUtc=playerStock.CheckedUtc,StockLines=playerStock.Lines};
Assert(pr.Apply(pp)?.Lines.Any(l=>l.EmptyPlace)==true,"empty player synchronizes with the stock snapshot");
Assert(!new StockLine{EmptyPlace=true,Location="Jugador",Quantity=1}.Valid(),"empty-place marker cannot carry a quantity");
Console.WriteLine($"{checks} checks passed. Test files: {folder}");




