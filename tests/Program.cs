using ScheduleIStats;
int checks=0;
void Assert(bool ok,string name) { if(!ok) throw new Exception(name); Console.WriteLine("PASS "+name); checks++; }
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
Console.WriteLine($"{checks} checks passed. Test files: {folder}");
