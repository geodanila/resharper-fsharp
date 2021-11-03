package com.jetbrains.rider.test.cases.projectModel

import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.enums.CoreVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class ReferencesOrderTest : BaseTestWithSolution() {
    override fun getSolutionDirectoryName() = "ReferencesOrder"

    override val waitForCaches = true
    override val restoreNuGetPackages = true

    private val fcsHost get() = project.solution.rdFSharpModel.fsharpTestHost

    @Test
    fun testReferencesOrder() {
        val references = fcsHost.dumpSingleProjectLocalReferences.sync(Unit)
        assert(references.equals(listOf("Library1.dll", "Library2.dll")))
    }
}
