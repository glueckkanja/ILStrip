Add this XML Element to the *.csproj File and Customize It:

  <Target Name="AfterBuild" Condition=" '$(Configuration)' == 'Release' ">
    <ILStrip InputFileName="$(TargetPath)" OutputFileName="$(TargetDir)MergedStripped\$(TargetFileName)" Verbose="" 
             KeepTypes="" KeepResources="" RemoveResources="" RenameResources="" />
  </Target>
