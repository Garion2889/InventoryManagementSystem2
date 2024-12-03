Module ResusableCodes

    Public Sub LockButtons(ParamArray buttons() As Button)
        For Each btn In buttons
            btn.Enabled = False
        Next
    End Sub

    Public Sub UnlockButtons(ParamArray buttons() As Button)
        For Each btn In buttons
            btn.Enabled = True
        Next
    End Sub

End Module
