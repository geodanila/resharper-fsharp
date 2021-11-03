package com.jetbrains.rider.test.cases.templates.core

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_6)
class FSharpTemplatesTestNet6 : FSharpTemplatesTestCore() {
    fun classLibCoreTemplate() = classLibCoreTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0)
    )

    fun classLibNetCoreAppTemplate() = classLibNetCoreAppTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            targetFramework = "net6.0")
    )

    fun consoleAppCoreTemplate() = consoleAppCoreTemplate(
        CoreTemplateTestArgs(expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            expectedOutput = "Hello from F#", breakpointFile = "Program.fs", breakpointLine = 4)
    )

    fun xUnitCoreTemplate() = xUnitCoreTemplate(
        CoreTemplateTestArgs(
            expectedNumOfAnalyzedFiles = 1, expectedNumOfSkippedFiles = 0,
            breakpointFile = "Tests.fs", breakpointLine = 8
        )
    )
}
