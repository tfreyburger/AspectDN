// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class CS5AspectChecker : CSAspectChecker
    {
        CS5AspectCoreCompilation _CoreCompilation;
        internal CS5AspectChecker(CS5AspectCoreCompilation coreCompilation)
        {
            _CoreCompilation = coreCompilation;
        }

#region #advice
        internal void AdviceTypeMembersDeclaration(AdviceTypeMembersDeclarationAspect typeMembersAdvice)
        {
            if (typeMembersAdvice.Identifier == null)
            {
                typeMembersAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", typeMembersAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceCodeDeclaration(AdviceCodeDeclarationAspect codeAdvice)
        {
            if (codeAdvice.Identifier == null)
            {
                codeAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", codeAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceChangeValueDeclaration(AdviceChangeValueDeclarationAspect changeValueAdvice)
        {
            if (changeValueAdvice.Identifier == null)
            {
                changeValueAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", changeValueAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceInterfaceMembersDeclaration(AdviceInterfaceMembersDeclarationAspect interfaceMemberAdvice)
        {
            if (interfaceMemberAdvice.Identifier == null)
            {
                interfaceMemberAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", interfaceMemberAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceEnumMembersDeclaration(AdviceEnumMembersDeclarationAspect enumMembersAdvice)
        {
            if (enumMembersAdvice.Identifier == null)
            {
                enumMembersAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", enumMembersAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceConstructorDeclaration(AdviceConstructorDeclarationAspect constructorAdvice)
        {
        }

        internal void AdviceDestructorDeclaration(AdviceDestructorDeclarationAspect destructorAdvice)
        {
        }

        internal void AdviceStaticConstructorDeclaration(AdviceStaticConstructorDeclarationAspect staticConstructorAdvice)
        {
        }

        internal void AdviceTypeDeclaration(AdviceTypesDeclarationAspect typeAdvice)
        {
            if (typeAdvice.Identifier == null)
            {
                typeAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", typeAdvice.SynToken.GetSourceLocation()));
            }
        }

        internal void AdviceAttributesDeclaration(AdviceAttributesDeclarationAspect attributesAdvice)
        {
            if (attributesAdvice.Identifier == null)
            {
                attributesAdvice.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAdviceIdentifier", attributesAdvice.SynToken.GetSourceLocation()));
            }
        }

#endregion
#region #pointcut
        internal void PointcutDeclaration(PointcutDeclarationAspect pointcut)
        {
            if (pointcut.Identifier == null)
            {
                pointcut.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoPointcutIdentifier", pointcut.SynToken.GetSourceLocation()));
            }
        }

#endregion
#region #aspect
        internal void AspectInheritDeclaration(InheritDeclarationAspect inheritAspect)
        {
            if (inheritAspect.Identifier == null)
            {
                inheritAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoPointcutIdentifier", inheritAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectCodeDeclaration(AspectCodeDeclarationAspect codeAspect)
        {
            if (codeAspect.Identifier == null)
            {
                codeAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", codeAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectChangeValueDeclaration(AspectChangeValueDeclarationAspect changeValueAspect)
        {
            if (changeValueAspect.Identifier == null)
            {
                changeValueAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", changeValueAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectTypeMembersDeclaration(AspectTypeMembersDeclarationAspect typeMembersAspect)
        {
            if (typeMembersAspect.Identifier == null)
            {
                typeMembersAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", typeMembersAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectInterfaceMembersDeclaration(AspectInterfaceMembersDeclarationAspect interfacemembersAspect)
        {
            if (interfacemembersAspect.Identifier == null)
            {
                interfacemembersAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", interfacemembersAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectEnumMembersDeclaration(AspectEnumMembersDeclarationAspect enumMemberAspect)
        {
            if (enumMemberAspect.Identifier == null)
            {
                enumMemberAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", enumMemberAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectTypeDeclaration(AspectTypesDeclarationAspect typeAspect)
        {
            if (typeAspect.Identifier == null)
            {
                typeAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", typeAspect.SynToken.GetSourceLocation()));
            }
        }

        internal void AspectAttributesDeclaration(AspectAttributesDeclarationAspect attributeAspect)
        {
            if (attributeAspect.Identifier == null)
            {
                attributeAspect.SetOnError();
                _CoreCompilation.Errors.Add(AspectDNErrorFactory.GetCompilerError("NoAspectIdentifier", attributeAspect.SynToken.GetSourceLocation()));
            }
        }
#endregion
    }
}