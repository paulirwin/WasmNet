;; invoke: les
;; expect: (i32:1)

(module
    (memory 1)
    (func (export "les") (result i32)
        i32.const 42
        i32.const 42
        i32.le_s
    )
)