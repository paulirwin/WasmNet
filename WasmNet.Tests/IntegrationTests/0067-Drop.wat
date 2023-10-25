;; invoke: drop
;; expect: (i32:42)

(module
    (func (export "drop") (result i32) 
        i32.const 0
        drop
        i32.const 42
    )
)