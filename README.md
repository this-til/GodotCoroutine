# RegisterSystem

一个管理对象的创建，注册和关系的工具

## 如何使用

    > C#
    public class Demo {
    public static void main() {
    RegisterSystem registerSystem = new RegisterSystem();
    //添加程序集
    registerSystem.initAddAllManagedAssembly(typeof(Demo).Assembly);
    //初始化
    registerSystem.initRegisterSystem();
    
            foreach (RegisterManage registerManage in registerSystem.getAllRegisterManage()) {
                Console.WriteLine($"RegisterManage:{registerManage.name}");
                foreach (RegisterBasics registerBasics in registerManage.forAll_erase()) {
                    Console.WriteLine(registerBasics.name);
                }
            }
        }
    
        /// <summary>
        /// 创建管理类
        /// </summary>
        public class TestRegisterManage : RegisterManage<TestRegister> {
            public static TestRegister test1 { get; protected set; }
            public static TestRegister test2 { get; protected set; }
        }
    
        public class Test2RegisterManage : RegisterManage<TestRegister> {
            public static TestRegister test3 { get; protected set; }
            public static TestRegister test4 { get; protected set; }
    
            /// <summary>
            /// 指定TestRegisterManage是自己的超集
            /// 会自动将自身注册项往上添加
            /// </summary>
            public override Type getBasicsRegisterManageType() => typeof(TestRegisterManage);
        }
    
        /// <summary>
        /// 创建注册类
        /// </summary>
        public class TestRegister : RegisterBasics {
        }
    }

结果:

    RegisterManage:TestRegisterManage
    test1
    test2
    test3
    test4
    RegisterManage:Test2RegisterManage
    test3
    test4



