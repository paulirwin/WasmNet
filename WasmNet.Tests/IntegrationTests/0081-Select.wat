;; invoke: select
;; expect: (i32:42)

(module
    (memory 1)
    (func (export "select") (result i32)
        i32.const 42
        i32.const 0
        i32.const 1
        select
    )
)