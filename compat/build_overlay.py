import shutil,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=root/"compat/source"
output=root/"compat/build-output"
subprocess.run(["dotnet","build",str(source/"vendor/r36s-a11y-sdk/R36S.A11y/R36S.A11y.csproj"),"-c","Release","-o",str(output)],check=True)
overlay=root/"overlay/engine"; overlay.mkdir(parents=True,exist_ok=True)
for name in ["R36S.A11y.dll"]: shutil.copyfile(output/name,overlay/name)
