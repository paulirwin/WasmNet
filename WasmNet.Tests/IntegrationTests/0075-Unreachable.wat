;; invoke: unreachable
;; expect_trap: UnreachableException

(module
    (func (export "unreachable") (result i32) 
        i32.const 0
        unreachable
    )
)